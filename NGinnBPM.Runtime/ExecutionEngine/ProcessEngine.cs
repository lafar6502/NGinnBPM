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
using NGinnBPM.Runtime.Services;

namespace NGinnBPM.Runtime.ExecutionEngine
{

    /// <summary>
    /// Process execution engine
    /// 
    /// </summary>
    public class ProcessEngine 
    {
        public Services.ITaskInstancePersister TaskPersister { get; set; }
        public Services.IDbSessionFactory SessionFactory { get; set; }
        public IProcessPackageRepo PackageRepository { get; set; }
        public IMessageBus MessageBus { get; set; }
        public IServiceResolver ServiceResolver { get; set; }
        private static Logger log = LogManager.GetCurrentClassLogger();

        public TaskPersistenceMode DefaultPersistenceMode { get; set; }

        public ProcessEngine()
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
                ps.TaskPersister.SaveNew(pi);
                log.Info("\n --- Created process {0} instance {1}. Data: {2}", pi.ProcessDefinitionId, pi.InstanceId, Jsonizer.ToJsonString(inputData));
                pi.Enable(inputData);
                pi.Deactivate();
                ps.TaskPersister.Update(pi); 
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

        protected void test()
        {
            Transaction t;
        }

        protected void ReadTask(string instanceId, Action<TaskInstance> act)
        {
            RunProcessTransaction(this.DefaultPersistenceMode, ps =>
            {
                var ti = ps.TaskPersister.GetForRead(instanceId);
                var pd = this.GetProcessDef(ti.ProcessDefinitionId);
                var pscript = this.GetProcessScriptRuntime(ti.ProcessDefinitionId);
                ti.Activate(ps, pd, pscript);
                act(ti);
                ti.Deactivate();
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

                    var ti = ps.TaskPersister.GetForUpdate(instanceId);
                    if (ti == null)
                    {
                        log.Warn("Task instance not found: {0}", instanceId);
                        var pti = (CompositeTaskInstance) ps.TaskPersister.GetForRead(InstanceId.GetParentTaskInstanceId(instanceId));
                        if (pti == null) throw new Exception("Task instance not found and no parent. Instance ID: " + instanceId);
                        var tin = pti.GetChildTransitionInfo(instanceId);
                        if (tin == null) throw new Exception("Task instance not found anywhere: " + instanceId);
                        log.Info("Task instance not found, parent says that status is {0}", tin.Status);
                        return;
                    };
                    var pd = this.GetProcessDef(ti.ProcessDefinitionId);
                    var pscript = this.GetProcessScriptRuntime(ti.ProcessDefinitionId);
                    ti.Activate(ps, pd, pscript);
                    var pstat = ti.Status;
                    act(ti);
                    OnTaskInstanceStatusChange(ti, pstat);
                    ti.Deactivate();
                    ps.TaskPersister.Update(ti);
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
                if (!ti.IsAlive)
                {
                    log.Warn("Trying to cancel an inactive task {0} [{1}], status: {2}", ti.TaskId, ti.InstanceId, ti.Status);
                }
                ti.Cancel(reason);
            });
        }

        public void SelectTask(string instanceId)
        {
            UpdateTask(instanceId, ti =>
            {
                if (ti.Status == TaskStatus.Selected) return;
                if (!ti.IsAlive)
                {
                    log.Warn("Trying to select an inactive task {0} [{1}], status: {2}", ti.TaskId, ti.InstanceId, ti.Status);
                }
                
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
                var pstat = ti.Status;
                if (ti.Status != TaskStatus.Enabling &&
                    ti.Status != TaskStatus.Enabled &&
                    ti.Status != TaskStatus.Selected &&
                    ti.Status != TaskStatus.Cancelling)
                    throw new ProcessModel.Exceptions.InvalidTaskStatusException();
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

        public static void InDbTransaction(IDbSessionFactory factory, Action<DbSession> act)
        {
            if (DbSession.Current != null)
            {
                act(DbSession.Current);
            }
            else
            {
                var dbs = factory.OpenSession(MessageBusContext.ReceivingConnection);
                try
                {
                    DbSession.Current = dbs;
                    act(dbs);
                }
                finally
                {
                    DbSession.Current = null;
                    dbs.Dispose();
                }
            }
        }

        protected void InDbTransaction(Action<DbSession> act)
        {
            InDbTransaction(SessionFactory, act);
        }

        protected void InSystemTransaction(Action act)
        {
            if (Transaction.Current == null) throw new Exception("System transaction required (transaction scope)");

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
                    var lid = Transaction.Current.TransactionInformation.LocalIdentifier;
                    log.Debug("Opened transaction scope {0}", lid);
                    act();
                    ts.Complete();
                    log.Debug("Completed transaction {0}", lid);
                }
            }
        }



        protected void PumpMessages(ProcessSession ps)
        {
            var queue = ps.SyncQueue;
            while (queue.Count > 0)
            {
                var m = queue.Dequeue();
                if (m is TaskExecEvent)
                {
                    var te = m as TaskExecEvent;
                    if (te.FromTaskInstanceId == te.FromProcessInstanceId || InstanceId.IsSameProcessInstance(te.ParentTaskInstanceId, te.FromProcessInstanceId))
                    {
                        DeliverTaskExecEvent(te);
                    }
                    else
                    {
                        MessageBus.Notify(te);
                    }
                }
                else if (m is TaskControlCommand)
                {
                    var tc = m as TaskControlCommand;
                    if (InstanceId.IsSameProcessInstance(tc.FromProcessInstanceId, tc.ToTaskInstanceId))
                    {
                        DeliverTaskControlMessage(tc);
                    }
                    else
                    {
                        MessageBus.Notify(tc);
                    }
                }
                else throw new Exception("Unexpected message in queue");
            }

           
        }


        /// <summary>
        /// Execute a process transaction
        /// Warning: you need to execute this method inside a Transaction (TransactionScope)
        /// this is how you control if the changes will be saved to the db.
        /// @param persMode - persistence mode
        /// 
        /// assume we have all process tasks in a single record
        /// so when performing modification, we need to limit our scope to that record (this process instance)
        /// and all messages leaving the process will be handled async
        /// so we divide our messages between in-process and inter-process
        /// inter-process are always async
        /// in-process are sync when they're task control messages 
        /// async when they are from send/receive message tasks (hmmm, this doesn't really matter when in proc)
        ///
        /// what about transactions?
        /// - we assume we're already inside a system transaction
        /// - we can get an external db connection. If we don't get it, we need to open it.
        /// - process session
        /// - other components should have an option to be notified about commit - use system.transactions api...
        /// </summary>
        public void RunProcessTransaction(TaskPersistenceMode persMode, Action<ProcessSession> act)
        {
            if (ProcessSession.Current != null)
            {
                act(ProcessSession.Current);
                return;
            }

            Queue<ProcessMessage> outgoing = null;
            InSystemTransaction(() =>
            {
                InDbTransaction(SessionFactory, dbs =>
                {
                    var pess = TaskPersister.OpenSession(dbs);
                    pess.PersistenceMode = persMode;
                    var ps = new ProcessSession(pess, MessageBus, ServiceResolver);
                    try
                    {
                        ProcessSession.Current = ps;
                        ps.MessageBus = MessageBus;
                        ps.TaskPersister = pess;
                        act(ps);
                        PumpMessages(ps);
                        outgoing = ps.AsyncQueue;
                    }
                    finally
                    {
                        ProcessSession.Current = null;
                        ps.Dispose();
                    }
                    pess.SaveChanges();
                    pess.Dispose();
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
            throw new NotImplementedException();
        }

        #region internals, event handlers

        internal void DeliverTaskExecEvent(TaskExecEvent ev)
        {
            if (string.IsNullOrEmpty(ev.ParentTaskInstanceId))
            {
                if (ev.FromTaskInstanceId == ev.FromProcessInstanceId)
                {
                    //process-level events
                    UpdateTask(ev.FromProcessInstanceId, ti =>
                    {
                        var pi = ti as ProcessInstance;
                        if (pi == null) throw new Exception("Process instance expected for id=" + ev.FromProcessInstanceId);
                        if (ev is TaskCompleted)
                        {
                            MessageBus.Notify(new TaskExecutionEvents.Process.ProcessCompleted {
                                InstanceId = ev.FromProcessInstanceId,
                                DefinitionId = pi.ProcessDefinitionId,
                                Timestamp = DateTime.Now
                            });
                        }
                        else if (ev is TaskFailed)
                        {
                            MessageBus.Notify(new TaskExecutionEvents.Process.ProcessFailed
                            {
                                InstanceId = ev.FromProcessInstanceId,
                                DefinitionId = pi.ProcessDefinitionId,
                                Timestamp = DateTime.Now
                            });
                        }
                        else if (ev is TaskCancelled)
                        {
                            MessageBus.Notify(new TaskExecutionEvents.Process.ProcessCancelled
                            {
                                InstanceId = ev.FromProcessInstanceId,
                                DefinitionId = pi.ProcessDefinitionId,
                                Timestamp = DateTime.Now
                            });
                        }
                        else if (ev is TaskEnabled)
                        {
                            MessageBus.Notify(new TaskExecutionEvents.Process.ProcessStarted
                            {
                                InstanceId = ev.FromProcessInstanceId,
                                DefinitionId = pi.ProcessDefinitionId,
                                Timestamp = DateTime.Now
                            });
                        }
                        else throw new Exception("Unexpected event " + ev.GetType().Name);
                    });
                }
                else
                {
                    log.Warn("Event has no parent but sender task {0} is not process instance {1}", ev.FromTaskInstanceId, ev.FromProcessInstanceId);
                }
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
            ps.TaskPersister.SaveNew(ti);
            ti.Activate(ps, pd, pscript);
            if (ti.Status != TaskStatus.Enabling)
            {
                log.Warn("STATUS!");
            }
            var prevStat = ti.Status;
            if (msg is EnableMultiChildTask)
            {
                ((MultiTaskInstance)ti).Enable(((EnableMultiChildTask)msg).MultiInputData);
            }
            else
            {
                ti.Enable(msg.InputData);
            }

            OnTaskInstanceStatusChange(ti, prevStat);
            
            ti.Deactivate();
            ps.TaskPersister.Update(ti);
            return ti.InstanceId;
        }

        class TaskStatusTransition
        {
            public TaskStatus From;
            public TaskStatus To;
        }

        private T FillTaskEvent<T>(TaskInstance ti, T ev) where T : TaskExecEvent
        {
            ev.FromProcessInstanceId = ti.ProcessInstanceId;
            ev.FromTaskInstanceId = ti.InstanceId;
            ev.ParentTaskInstanceId = ti.ParentTaskInstanceId;
            return ev;
        }

        private void OnTaskInstanceStatusChange(TaskInstance ti, TaskStatus previousStatus)
        {
            if (previousStatus == ti.Status) return; //no status change - no message
            log.Info("Task status change {0} ({1}) Status: {2} => {3}", ti.InstanceId, ti.TaskId, previousStatus, ti.Status);
            var ps = ProcessSession.Current;
            
            TaskStatusTransition[] tts = new TaskStatusTransition[] {
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Enabled},
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Selected},
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Completed},
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Failed},
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Cancelling},
                new TaskStatusTransition {From = TaskStatus.Enabling, To = TaskStatus.Cancelled},
                new TaskStatusTransition {From = TaskStatus.Enabled, To = TaskStatus.Selected},
                new TaskStatusTransition {From = TaskStatus.Enabled, To = TaskStatus.Completed},
                new TaskStatusTransition {From = TaskStatus.Enabled, To = TaskStatus.Failed},
                new TaskStatusTransition {From = TaskStatus.Enabled, To = TaskStatus.Cancelling},
                new TaskStatusTransition {From = TaskStatus.Enabled, To = TaskStatus.Cancelled},
                new TaskStatusTransition {From = TaskStatus.Selected, To = TaskStatus.Completed},
                new TaskStatusTransition {From = TaskStatus.Selected, To = TaskStatus.Failed},
                new TaskStatusTransition {From = TaskStatus.Selected, To = TaskStatus.Cancelling},
                new TaskStatusTransition {From = TaskStatus.Selected, To = TaskStatus.Cancelled},
                new TaskStatusTransition {From = TaskStatus.Cancelling, To = TaskStatus.Cancelled},
                new TaskStatusTransition {From = TaskStatus.Cancelling, To = TaskStatus.Failed}
            };
            var tt = tts.First(x => x.From == previousStatus && x.To == ti.Status);
            if (tt == null) throw new ProcessModel.Exceptions.InvalidTaskStatusException().SetInstanceId(ti.InstanceId).SetPermanent(true);

            switch (ti.Status)
            {
                case TaskStatus.Enabling:
                    //do nothing..
                    break;
                case TaskStatus.Enabled:
                    //notify task enabled
                    ps.NotifyTaskEvent(FillTaskEvent(ti, new TaskEnabled()));
                    break;
                case TaskStatus.Selected:
                    //notify task selected
                    ps.NotifyTaskEvent(FillTaskEvent(ti, new TaskSelected()));
                    break;
                case TaskStatus.Completed:
                    
                    if (ti is MultiTaskInstance)
                    {
                        ps.NotifyTaskEvent(FillTaskEvent(ti, new MultiTaskCompleted { 
                            MultiOutputData = ((MultiTaskInstance) ti).GetMultiOutputData()
                        }));
                    }
                    else
                    {
                        ps.NotifyTaskEvent(FillTaskEvent(ti, new TaskCompleted
                        {
                            OutputData = ti.GetOutputData()
                        }));
                    }
                    log.Debug("Task completed: {0}", ti.ToString());
                    break;
                case TaskStatus.Failed:
                    ps.NotifyTaskEvent(FillTaskEvent(ti, new TaskFailed { ErrorInfo = ti.StatusInfo, IsExpected = true }));
                    break;
                default:
                    throw new ProcessModel.Exceptions.InvalidTaskStatusException().SetInstanceId(ti.InstanceId).SetPermanent(true);
                    break;
            }
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

        public Dictionary<string, object> GetTaskData(string instanceId)
        {
            Dictionary<string, object> ret = null;
            this.ReadTask(instanceId, ti =>
            {
                ret = new Dictionary<string,object>(ti.TaskData);
            });
            return ret;
        }

        public CompositeTaskInstanceInfo GetTaskInstanceInfo(string instanceId)
        {
            CompositeTaskInstanceInfo ret = null;
            RunProcessTransaction(this.DefaultPersistenceMode, ps =>
            {
                CompositeTaskInstance cti = (CompositeTaskInstance)ps.TaskPersister.GetForRead(instanceId);
                CompositeTaskInstanceInfo rt = new CompositeTaskInstanceInfo();
                rt.InstanceId = cti.InstanceId;
                rt.TaskId = cti.TaskId;
                rt.ProcessDefinitionId = cti.ProcessDefinitionId;
                rt.ProcessInstanceId = cti.ProcessInstanceId;
                rt.Marking = cti.Marking.Where(x => x.Value > 0).Select(x => x.Key).ToList();
                rt.ActiveTasks = cti.ActiveTasks.Select(x => x.TaskId).ToList();
                rt.Status = cti.Status;
                ret = rt;
            });
            return ret;
        }



        
    }
}
