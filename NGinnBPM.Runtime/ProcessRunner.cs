using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using System.Transactions;
using NGinnBPM.ProcessModel;
using NGinnBPM.Runtime.Tasks;
using NLog;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// 
    /// </summary>
    public enum ProcessStateSaveStrategy
    {
        DontPersistAnything = 0,
        PersistAliveTasksOnly = 1,
        PersistAll = 2
    };

    /// <summary>
    /// Process runner does run the processes by pushing them forward in a transactional manner.
    /// A single process transaction does as much as it can, without bothering parent tasks (they don't get mixed into child task's transaction).
    /// This way we can execute a whole process in a single transaction if we want.
    /// </summary>
    public class ProcessRunner 
    {
        public Services.ITaskInstancePersister TaskPersister { get; set; }
        public Services.IDbSessionFactory SessionFactory { get; set; }
        public IProcessPackageRepo PackageRepository { get; set; }
        public IMessageBus MessageBus { get; set; }
        private static Logger log = LogManager.GetCurrentClassLogger();

        public string StartProcess(string definitionId, Dictionary<string, object> inputData)
        {
            string ret = null;
            RunProcessTransaction(ps =>
            {
                var pd = this.GetProcessDef(definitionId);
                var pscript = this.GetProcessScriptRuntime(definitionId);

                string instanceId = Guid.NewGuid().ToString("N");
                ProcessInstance pi = new ProcessInstance
                {
                    InstanceId = instanceId,
                    ProcessDefinitionId = definitionId,
                    ProcessInstanceId = instanceId,
                    TaskId = pd.Body.Id
                };
                pi.Activate(ps, pd, pscript);
                ps.PersisterSession.SaveNew(pi);
                pi.Enable(inputData);
                pi.Deactivate();
                ps.PersisterSession.Update(pi); 
                ret = pi.InstanceId;
            });
            return ret;
        }

        public void UpdateTaskData(string instanceId, Dictionary<string, object> updatedData)
        {
            UpdateTask(instanceId, ti =>
            {
                foreach (string k in updatedData.Keys)
                {
                    ti.TaskData[k] = updatedData[k];
                }
            });
        }

        protected void UpdateTask(string instanceId, Action<TaskInstance> act)
        {
            RunProcessTransaction(ps =>
            {
                var ti = ps.PersisterSession.GetForUpdate(instanceId);
                var pd = this.GetProcessDef(ti.ProcessDefinitionId);
                var pscript = this.GetProcessScriptRuntime(ti.ProcessDefinitionId);
                ti.Activate(ps, pd, pscript);
                act(ti);
                ti.Deactivate();
                ps.PersisterSession.Update(ti);
            });
        }
        
        public void CancelTask(string instanceId, string reason)
        {
            UpdateTask(instanceId, ti =>
            {
                ti.Cancel(reason);
            });
        }

        public void SelectTask(string instanceId)
        {
            UpdateTask(instanceId, ti =>
            {
                ti.Select();
            });
        }

        public void ForceCompleteTask(string instanceId, Dictionary<string, object> updatedData)
        {
            UpdateTask(instanceId, ti =>
            {
                ti.ForceComplete(updatedData);
            });
        }

        public void ForceFailTask(string instanceId, string reason)
        {
            UpdateTask(instanceId, ti =>
            {
                ti.ForceFail(reason);
            });
        }

        protected ProcessDef GetProcessDef(string definitionId)
        {
            return ProcessSession.Current.GetOrAddSessionData("_ProcessDef_" + definitionId, () => PackageRepository.GetProcessDef(definitionId));
        }

        protected IProcessScriptRuntime GetProcessScriptRuntime(string definitionId)
        {
            return ProcessSession.Current.GetOrAddSessionData("_ProcessScript_" + definitionId, () => PackageRepository.GetScriptRuntime(definitionId));
        }

        protected void InDbTransaction(Action<DbSession> act)
        {
            if (DbSession.Current != null)
            {
                act(DbSession.Current);
            }
            else
            {
                using (var dbs = SessionFactory.OpenSession())
                {
                    DbSession.Current = dbs;
                    act(dbs);
                    DbSession.Current = null;
                }
            }
        }

        protected void InSystemTransaction(Action act)
        {
            if (Transaction.Current != null)
            {
                act();
            }
            else
            {
                TransactionOptions to = new TransactionOptions { 
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(60)
                };
                using (var ts = new TransactionScope(TransactionScopeOption.Required, to))
                {
                    act();
                    ts.Complete();
                }
            }
        }

        protected void RunProcessTransaction(Action<ProcessSession> act)
        {
            if (ProcessSession.Current != null)
            {
                act(ProcessSession.Current);
                return;
            }

            InSystemTransaction(() =>
            {
                InDbTransaction(dbs =>
                {
                    using (var pess = TaskPersister.OpenSession(dbs))
                    {
                        Services.TaskPersisterSession.Current = pess;
                        using (var ps = ProcessSession.CreateNew(this, pess))
                        {
                            ProcessSession.Current = ps;
                            act(ps);
                            ps.PumpMessages();
                        }
                        pess.SaveChanges();
                        Services.TaskPersisterSession.Current = null;
                    }
                });
            });
        }

        #region internals, event handlers

        internal void DeliverTaskExecEvent(TaskExecEvent ev)
        {
            if (string.IsNullOrEmpty(ev.ParentTaskInstanceId))
            {
                log.Info("event has no parent: {0}", ev);
                return; //TODO handle process-level events
            }
            UpdateTask(ev.ParentTaskInstanceId, ti =>
            {
                var cti = ti as CompositeTaskInstance;
                cti.HandleChildTaskEvent(ev);
            });
        }

        internal void DeliverTaskControlMessage(TaskControlMessage tcm)
        {
            RunProcessTransaction(ps =>
            {
                if (tcm is EnableChildTask)
                {
                    EnableChildTask(tcm as EnableChildTask);
                    return;
                }
                throw new NotImplementedException();
            });
        }

        protected string EnableChildTask(EnableChildTask msg)
        {
            var ps = ProcessSession.Current;
            var pd = this.GetProcessDef(msg.ProcessDefinitionId);
            var pscript = this.GetProcessScriptRuntime(msg.ProcessDefinitionId);
            var td = pd.GetRequiredTask(msg.TaskId);
            TaskInstance ti;
            if (msg.MultiInputData != null)
            {
                if (!td.IsMultiInstance) throw new Exception("Task is not multi-instance: " + td.Id);
                ti = CreateTaskInstance(td); //TODO: multi, multi!
            }
            else
            {
                ti = CreateTaskInstance(td);
            }
            ti.ParentTaskInstanceId = msg.FromTaskInstanceId;
            ti.ProcessInstanceId = msg.FromProcessInstanceId;
            ti.InstanceId = string.IsNullOrEmpty(msg.CorrelationId) ? null : msg.CorrelationId;
            ti.ProcessDefinitionId = msg.ProcessDefinitionId;
            ti.TaskId = msg.TaskId;
            ps.PersisterSession.SaveNew(ti);
            ti.Activate(ps, pd, pscript);
            ti.Enable(msg.InputData);
            ti.Deactivate();
            ps.PersisterSession.Update(ti);
            return ti.InstanceId;
        }

        protected TaskInstance CreateTaskInstance(TaskDef td)
        {
            if (td is CompositeTaskDef) return new CompositeTaskInstance();
            AtomicTaskDef at = td as AtomicTaskDef;
            switch (at.TaskType)
            {
                case NGinnTaskType.Empty:
                    return new EmptyTaskInstance();
                default:
                    return new EmptyTaskInstance();
            }
        }
        #endregion
    }
}
