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
using System.Threading;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// 
    /// </summary>
    public enum TaskPersistenceMode
    {
        /// <summary>
        /// Don't persist task state at all
        /// </summary>
        DontPersistAnything = 0,
        /// <summary>
        /// Persist tasks only if they didn't complete in a single transaction.
        /// </summary>
        PersistAliveTasksOnly = 1,
        /// <summary>
        /// Persist everything.
        /// </summary>
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
        public IServiceResolver ServiceResolver { get; set; }
        private static Logger log = LogManager.GetCurrentClassLogger();

        public TaskPersistenceMode DefaultPersistenceMode { get; set; }

        public ProcessRunner()
        {
            DefaultPersistenceMode = TaskPersistenceMode.PersistAliveTasksOnly;
        }

        public string StartProcess(string definitionId, Dictionary<string, object> inputData)
        {
            
            string ret = null;
            RunProcessTransaction(this.DefaultPersistenceMode, ps =>
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
            RunProcessTransaction(this.DefaultPersistenceMode, ps =>
            {
                string ol = MappedDiagnosticsContext.Get("NG_TaskInstanceId");
                try
                {
                    MappedDiagnosticsContext.Set("NG_TaskInstanceId", instanceId);
                    var ti = ps.PersisterSession.GetForUpdate(instanceId);
                    var pd = this.GetProcessDef(ti.ProcessDefinitionId);
                    var pscript = this.GetProcessScriptRuntime(ti.ProcessDefinitionId);
                    ti.Activate(ps, pd, pscript);
                    act(ti);
                    ti.Deactivate();
                    ps.PersisterSession.Update(ti);
                }
                finally
                {
                    MappedDiagnosticsContext.Set("NG_TaskInstanceId", ol);
                }
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

        protected void RunProcessTransaction(TaskPersistenceMode persMode, Action<ProcessSession> act)
        {
            if (ProcessSession.Current != null)
            {
                act(ProcessSession.Current);
                return;
            }

            IEnumerable<ProcessMessage> outgoing = null;
            InSystemTransaction(() =>
            {
                InDbTransaction(dbs =>
                {
                    using (var pess = TaskPersister.OpenSession(dbs))
                    {
                        pess.PersistenceMode = persMode;
                        Services.TaskPersisterSession.Current = pess;
                        using (var ps = ProcessSession.CreateNew(this, pess))
                        {
                            ProcessSession.Current = ps;
                            act(ps);
                            ps.PumpMessages();
                            outgoing = ps.GetOutgoingAsyncMessages();
                        }
                        pess.SaveChanges();
                        Services.TaskPersisterSession.Current = null;
                    }
                });
            });
            if (outgoing != null)
            {
                foreach (var pm in outgoing)
                {
                    SendLocalAsyncMessage(pm);
                }
            }
        }

        protected void SendLocalAsyncMessage(ProcessMessage pm)
        {
            System.Threading.Tasks.Task.Factory.StartNew((q) =>
            {
                ProcessMessage m = q as ProcessMessage;
                try
                {
                    log.Warn("Handling async message {0} from {1}", m.GetType().Name, m.FromTaskInstanceId);
                    HandleLocalAsyncMessage(pm);
                }
                catch (Exception ex)
                {
                    //TODO: some error handling here, for example report 'TaskFailed' for EnableTask
                    log.Error("Error handling local async message {0} from {1}: {2}", m.GetType().Name, m.FromTaskInstanceId);
                }
            }, pm);
        }

        protected void HandleLocalAsyncMessage(ProcessMessage pm)
        {
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
                ti.HandleTaskExecEvent(ev);
            });
        }

        internal void DeliverTaskControlMessage(TaskControlCommand tcm)
        {
            RunProcessTransaction(this.DefaultPersistenceMode,ps =>
            {
                if (tcm is EnableChildTask)
                {
                    EnableChildTask(tcm as EnableChildTask);
                    return;
                }
                else if (tcm is CancelTask)
                {
                    CancelTask(tcm.ToTaskInstanceId, "");
                    return;
                }
                else if (tcm is SelectTask)
                {
                    SelectTask(tcm.ToTaskInstanceId);
                    return;
                }
                else if (tcm is FailTask)
                {
                    this.ForceFailTask(tcm.ToTaskInstanceId, ((FailTask)tcm).ErrorInfo);
                    return;
                }
                else if (tcm is ForceCompleteTask)
                {
                    this.ForceCompleteTask(tcm.ToTaskInstanceId, ((ForceCompleteTask)tcm).UpdateData);
                    return;
                }
                else throw new NotImplementedException(tcm.GetType().Name);
            });
        }

        

        protected string EnableChildTask(EnableChildTask msg)
        {
            var ps = ProcessSession.Current;
            var pd = this.GetProcessDef(msg.ProcessDefinitionId);
            var pscript = this.GetProcessScriptRuntime(msg.ProcessDefinitionId);
            var td = pd.GetRequiredTask(msg.TaskId);
            TaskInstance ti;
            if (msg is EnableMultiChildTask)
            {
                if (!td.IsMultiInstance) throw new Exception("Task is not multi-instance: " + td.Id);
                ti = new MultiTaskInstance();
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
            if (msg is EnableMultiChildTask)
            {
                ((MultiTaskInstance)ti).Enable(((EnableMultiChildTask)msg).MultiInputData);
            }
            else
            {
                ti.Enable(msg.InputData);
            }
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
                case NGinnTaskType.Timer:
                    return new TimerTaskInstance();
                case NGinnTaskType.Debug:
                    return new DebugTaskInstance();
                case NGinnTaskType.Manual:
                    return new ManualTaskInstance();
                case NGinnTaskType.SendMessage:
                    return new SendMessageTaskInstance();
                case NGinnTaskType.ReceiveMessage:
                    return new AwaitMessageTaskInstance();
                case NGinnTaskType.Custom:
                    if (string.IsNullOrEmpty(td.ImplementationClass)) throw new Exception("ImplementationClass missing");
                    Type t = Type.GetType(td.ImplementationClass);
                    var ti = (TaskInstance)Activator.CreateInstance(t);
                    return ti;
                default:
                    return new EmptyTaskInstance();
            }
        }
        #endregion

        public CompositeTaskInstanceInfo GetTaskInstanceInfo(string instanceId)
        {
            CompositeTaskInstanceInfo ret = null;
            RunProcessTransaction(this.DefaultPersistenceMode, ps =>
            {
                CompositeTaskInstance cti = (CompositeTaskInstance) ps.PersisterSession.GetForRead(instanceId);
                CompositeTaskInstanceInfo rt = new CompositeTaskInstanceInfo();
                rt.InstanceId = cti.InstanceId;
                rt.TaskId = cti.TaskId;
                rt.ProcessDefinitionId = cti.ProcessDefinitionId;
                rt.ProcessInstanceId = cti.ProcessInstanceId;
                rt.Marking = cti.Marking.Where(x => x.Value > 0).Select(x => x.Key).ToList();
                rt.ActiveTasks = cti.ActiveTasks.Select(x => x.TaskId).ToList();
                ret = rt;
            });
            return ret;
        }

        
    }
}
