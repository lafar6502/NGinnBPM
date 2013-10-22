using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.ProcessModel.Exceptions;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime.Tasks;
using NGinnBPM.Runtime;
using NGinnBPM.Runtime.Services;
using NLog;
using System.Diagnostics;
using System.Collections;
using System.Runtime.Serialization;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime.Tasks
{

    /// <summary>
    /// Multi-instance task...
    /// Problem 1. How to enable tasks - sequentially, or in a single transaction?
    /// Problem 2. How to handle child task failure/cancellation
    /// </summary>
    [DataContract]
    public class MultiTaskInstance : TaskInstance
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// If false, all child tasks must complete. If some child fails, whole multi task instance will fail also.
        /// If true, some of them can fail.
        /// </summary>
        [DataMember]
        //[TaskParameter(IsInput=true, Required=false)]
        public bool AllowChildTaskFailures { get; set; }

        [DataMember(IsRequired=true)]
        public List<TransitionInfo> ChildTransitions {get;set;}

        public MultiTaskInstance()
        {
            ChildTransitions = new List<TransitionInfo>();
        }

        public TransitionInfo GetTransitionInfo(string instanceId)
        {
            foreach (TransitionInfo ti in ChildTransitions)
            {
                if (ti.InstanceId == instanceId)
                    return ti;
            }
            return null;
        }

        public override void Enable(Dictionary<string, object> inputData)
        {
            List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();
            lst.Add(new Dictionary<string, object>(inputData));
            Enable(lst);
        }

       

        /// <summary>
        /// Do we have a race condition here?
        /// When enabling, the task is not yet persisted. So if it enables child tasks,
        /// we cannot accept messages from them until we are persisted as well...
        /// How to handle this???
        /// Current approach is to set a lock on instance ID so incoming child events will  have to wait
        /// until the lock is released...
        /// Problem: current task is not saved when enabling child tasks, however we are giving them parent
        /// id. Their parent is not in database, so they cannot access parent task instance...
        /// Solution - do it in async way........ not very performant, however. At least unless we use
        /// better messaging
        /// </summary>
        /// <param name="inputData"></param>
        public void Enable(ICollection<Dictionary<string, object>> inputData)
        {
            RequireActivation(true);
            //TODO: validate the data
            if (inputData.Count == 0)
                throw new TaskDataInvalidException(TaskId, InstanceId, "No input data for multi-instance task");

            foreach (Dictionary<string, object> dob in inputData)
            {
                TransitionInfo ti = new TransitionInfo();
                ti.Status = TransitionStatus.Enabling;
                ti.InstanceId = AllocateNewTaskInstanceId(TaskId);
                ChildTransitions.Add(ti);
                Context.SendTaskControlMessage(new EnableChildTask {
                    CorrelationId = ti.InstanceId,
                    FromProcessInstanceId = this.ProcessInstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    InputData = dob,
                    ProcessDefinitionId = this.ProcessDefinitionId,
                    TaskId = this.TaskId
                });
            }
            this.Status = TaskStatus.Enabled;
            EnabledDate = DateTime.Now;
            Context.NotifyTaskEvent(new TaskEnabled
            {
                FromTaskInstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId
            });
        }

        
       
        protected TransitionInfo GetTransition(string instanceId)
        {
            return ChildTransitions.FirstOrDefault(ti => ti.InstanceId == instanceId);
        }

        

        protected void OnTransitionStatusChanged(string transitionId)
        {
            if (IsAnyTransitionActiveYet())
            {
                return;
            }
            log.Info("There are no active transitions. Completing task.");
            if (Status == TaskStatus.Cancelling)
            {
                DefaultHandleTaskCancel("");
            }
            else
            {
                Debug.Assert(Status == TaskStatus.Enabled || Status == TaskStatus.Selected);
                List<Dictionary<string, object>> lst = new List<Dictionary<string, object>>();
                foreach (TransitionInfo ti in ChildTransitions)
                {
                    if (ti.Status == TransitionStatus.Completed)
                    {
                        Dictionary<string, object> dob = ti.OutputData;
                        if (dob == null) dob = new Dictionary<string, object>();
                        lst.Add(ti.OutputData);
                    }
                    else
                    {
                        log.Info("Skipping failed or cancelled child task: {0}", ti);
                    }
                }
                Dictionary<string, object> dob2 = new Dictionary<string, object>();
                dob2["multiInstanceResults"] = lst;
                DefaultHandleTaskCompletion(dob2);
            }
        }

        

        private void CancelRemainingActiveChildTasks()
        {
            foreach (TransitionInfo ti in ChildTransitions)
            {
                if (ti.Status == TransitionStatus.Enabled ||
                    ti.Status == TransitionStatus.Enabling ||
                    ti.Status == TransitionStatus.Started)
                {
                    ti.Status = TransitionStatus.Cancelling;
                    Context.SendTaskControlMessage(new CancelTask
                    {
                        FromProcessInstanceId = this.ProcessInstanceId,
                        FromTaskInstanceId = this.InstanceId,
                        ToTaskInstanceId = ti.InstanceId,
                        Reason = "parent cancel"
                    });
                }
                else if (ti.Status == TransitionStatus.FailedActive)
                {
                    ti.Status = TransitionStatus.Cancelled;
                }
                Debug.Assert(ti.Status == TransitionStatus.Completed || ti.Status == TransitionStatus.Failed || ti.Status == TransitionStatus.Cancelled || ti.Status == TransitionStatus.Cancelling);
            }
        }

        public override void Cancel(string reason)
        {
            lock(this)
            {
                if (Status == TaskStatus.Cancelled || Status == TaskStatus.Cancelling)
                    return;
                else if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected) throw new InvalidTaskStatusException(InstanceId, "Invalid status");
                CancelRemainingActiveChildTasks();
                if (!IsAnyTransitionActiveYet())
                {
                    base.DefaultHandleTaskCancel(reason);
                    Debug.Assert(Status == TaskStatus.Cancelled);
                }
                else
                {
                    Status = TaskStatus.Cancelling;
                }
            }
        }

        private bool IsAnyTransitionActiveYet()
        {
            foreach (TransitionInfo ti in ChildTransitions)
            {
                if (ti.IsTransitionActive) return true;
            }
            return false;
        }

        public override void ForceFail(string errorInformation)
        {
            CancelRemainingActiveChildTasks();
            this.DefaultHandleTaskFailure(errorInformation, true);
        }

        public override void ForceComplete(Dictionary<string, object> updatedData)
        {
            CancelRemainingActiveChildTasks();
            DefaultHandleTaskCompletion(updatedData);
        }

        /// <summary>
        /// creates a new task instance id for a child task.
        /// The id generated should be unique.
        /// This way composite tasks can control what ids are assigned to child tasks.
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        protected string AllocateNewTaskInstanceId(string taskId)
        {
            int n;
            lock (this)
            {
                n = ++ChildCounter;
            }
            string s = string.Format("{0}.{1}", InstanceId, n);
            if (this.GetTransitionInfo(s) != null) throw new Exception("Child task instance ID is duplicate. Oh my!");
            return s;
        }

        [DataMember]
        public int ChildCounter { get; set; }

        #region IMessageConsumer<TaskEnabledMessage> Members

        public void Handle(TaskEnabled message)
        {
            RequireActivation(true);
            TransitionInfo ti = GetTransition(message.CorrelationId);
            if (ti == null) throw new TaskRuntimeException("Child transition not found: " + message.CorrelationId).SetInstanceId(InstanceId);
            if (ti.Status == TransitionStatus.Enabling)
            {
                ti.Status = TransitionStatus.Enabled;
                if (ti.InstanceId != message.FromTaskInstanceId)
                {
                    log.Info("Child task instance id changed {0}->{1}", ti.InstanceId, message.FromTaskInstanceId);
                    ti.InstanceId = message.FromTaskInstanceId;
                }
            }
            else
            {
                log.Debug("Ignoring message: {0}", message);
            }
        }

        #endregion

        

        #region IMessageConsumer<TaskSelected> Members

        public void Handle(TaskSelected message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId) throw new Exception();
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) throw new Exception();
            lock (this)
            {
                log.Info("Child task {0} has been started", ti.InstanceId);
                if (ti.Status == TransitionStatus.Started)
                    return;
                if (ti.Status != TransitionStatus.Enabled && ti.Status != TransitionStatus.Enabling)
                {
                    log.Warn("Transition {0} ({1}) is not enabled: {2}", ti.InstanceId, ti.TaskId, ti.Status);
                    return;
                }
                ti.Status = TransitionStatus.Started;
                OnTransitionStatusChanged(ti.InstanceId);
            }
        }

        #endregion

        #region IMessageConsumer<TaskCompleted> Members

        public void Handle(TaskCompleted message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId) throw new Exception();
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) throw new Exception();
            lock (this)
            {
                log.Info("Child task {0} has been completed", ti.InstanceId);
                if (ti.Status == TransitionStatus.Completed)
                    return;
                if (!ti.IsTransitionActive)
                {
                    log.Warn("Transition {0} ({1}) is not active: {2}", ti.InstanceId, ti.TaskId, ti.Status);
                    return;
                }
                ti.Status = TransitionStatus.Completed;
                ti.OutputData = message.OutputData;
                if (ti.OutputData == null)
                {
                    log.Info("No output data returned from child task {0}", ti.InstanceId);
                    ti.OutputData = new Dictionary<string, object>();
                }
                OnTransitionStatusChanged(ti.InstanceId);
                return;
            }
        }

        #endregion

        #region IMessageConsumer<TaskCancelled> Members

        public void Handle(TaskCancelled message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId) throw new Exception();
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) throw new Exception();
            lock (this)
            {
                log.Info("Child task {0} has been cancelled", ti.InstanceId);
                if (ti.Status == TransitionStatus.Cancelled)
                    return;
                if (!ti.IsTransitionActive)
                {
                    log.Warn("Transition {0} ({1}) is not active: {2}", ti.InstanceId, ti.TaskId, ti.Status);
                    return;
                }
                ti.Status = TransitionStatus.Cancelled;
                OnTransitionStatusChanged(ti.InstanceId);
                return;
            }
        }

        #endregion

        #region IMessageConsumer<TaskFailed> Members

        public void Handle(TaskFailed message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId) throw new Exception();
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) throw new Exception();
            lock (this)
            {
                log.Info("Child task {0} has failed: {1}", ti.InstanceId, message.ErrorInfo);
                if (ti.Status == TransitionStatus.Failed)
                    return;
                if (!ti.IsTransitionActive)
                {
                    log.Warn("Transition {0} ({1}) is not active: {2}", ti.InstanceId, ti.TaskId, ti.Status);
                    return;
                }
                if (message.IsExpected)
                {
                    ti.Status = TransitionStatus.Failed;
                    if (!AllowChildTaskFailures)
                    {
                        DefaultHandleTaskFailure(message.ErrorInfo, true);
                        return;
                    }
                }
                else
                {
                    ti.Status = TransitionStatus.FailedActive;
                }
                OnTransitionStatusChanged(ti.InstanceId);
                return;
            }
        }

        #endregion

        
        #region IMessageConsumer<CancelTaskTimeout> Members

        /*public void Handle(CancelTaskTimeout message)
        {
            RequireActivation(true);
            if (Status != TaskStatus.Cancelling)
            {
                log.Info("Ignoring cancel timeout.");
                return;
            }
            if (IsAnyTransitionActiveYet())
            {
                log.Warn("Task {0} cancellation timeout. Ignoring child transitions that haven't yet cancelled and forcing the cancellation", InstanceId);
                DefaultHandleTaskCancelled();
                Debug.Assert(Status == TaskStatus.Cancelled);
            }
            else
            {
                //we should not be cancelling anymore - a bug!!!
                log.Error("Task {0}: cancellation timeout, but there are no active transitions. Task should have cancelled");
                Debug.Assert(false);
                DefaultHandleTaskCancelled();
            }
        }*/

        #endregion
    }
}
