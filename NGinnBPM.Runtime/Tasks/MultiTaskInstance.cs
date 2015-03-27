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
        public bool AllowChildTaskFailures { get; set; }

        [DataMember(IsRequired=true)]
        public List<TransitionInfo> ChildTransitions {get;set;}

        public MultiTaskInstance()
        {
            ChildTransitions = new List<TransitionInfo>();
        }

        public TransitionInfo GetTransitionInfo(string instanceId)
        {
            return ChildTransitions.FirstOrDefault(x => x.InstanceId == instanceId);
        }

        public override void Enable(Dictionary<string, object> inputData)
        {
            throw new NotImplementedException();
        }

        public override Dictionary<string, object> GetOutputData()
        {
            throw new Exception("Not supported in multi instance task. Use GetMultiOutputData");
        }

        public virtual List<Dictionary<string, object>> GetMultiOutputData()
        {
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
                    lst.Add(null);
                }
            }
            return lst;
        }

        /// <summary>
        /// </summary>
        /// <param name="inputData"></param>
        public void Enable(ICollection<Dictionary<string, object>> inputData)
        {
            RequireActivation(true);
            //TODO: validate the data
            if (inputData.Count == 0)
                throw new TaskDataInvalidException(TaskId, InstanceId, "No input data for multi-instance task");
            this.Status = TaskStatus.Enabling;
            foreach (Dictionary<string, object> dob in inputData)
            {
                TransitionInfo ti = new TransitionInfo();
                ti.Status = TransitionStatus.Enabling;
                ti.InstanceId = AllocateNewTaskInstanceId(TaskId);
                ChildTransitions.Add(ti);
                
                Context.EnableChildTask(new EnableChildTask {
                    CorrelationId = ti.InstanceId,
                    FromProcessInstanceId = this.ProcessInstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    ToTaskInstanceId = ti.InstanceId,
                    InputData = dob,
                    ProcessDefinitionId = this.ProcessDefinitionId,
                    TaskId = this.TaskId
                });
            }
        }

        
       
        protected TransitionInfo GetTransition(string instanceId)
        {
            return ChildTransitions.FirstOrDefault(ti => ti.InstanceId == instanceId);
        }

        

        protected void OnTransitionStatusChanged(string transitionId)
        {
            if (Status == TaskStatus.Enabling)
            {
                if (ChildTransitions.Any(x => x.Status == TransitionStatus.Enabled ||
                    x.Status == TransitionStatus.Started ||
                    x.Status == TransitionStatus.Completed))
                {
                    this.Status = TaskStatus.Enabled;
                    EnabledDate = DateTime.Now;
                }
            }

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
                var lst = GetMultiOutputData();
                Status = TaskStatus.Completed;
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
                    Context.CancelChildTask(new CancelTask
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
                    //we can just report cancellation
                    base.DefaultHandleTaskCancel(reason);
                    Debug.Assert(Status == TaskStatus.Cancelled);
                }
                else
                {
                    //wait for all subtasks to cancel
                    Status = TaskStatus.Cancelling;
                }
            }
        }

        private bool IsAnyTransitionActiveYet()
        {
            return ChildTransitions.Any(x => x.IsTransitionActive);
        }

        public override void ForceFail(string errorInformation)
        {
            //cancel all subtasks without waiting for cancellation to complete
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

        private void Handle(TaskEnabled message)
        {
            RequireActivation(true);
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) ti = GetTransition(message.CorrelationId);
            if (ti == null) throw new TaskRuntimeException("Child transition not found: " + message.CorrelationId).SetInstanceId(InstanceId);
            if (ti.Status == TransitionStatus.Enabling)
            {
                log.Info("MT Child transition {0} has been enabled", ti.InstanceId);
                ti.Status = TransitionStatus.Enabled;
                if (ti.InstanceId != message.FromTaskInstanceId)
                {
                    log.Info("Child task instance id changed {0}->{1}", ti.InstanceId, message.FromTaskInstanceId);
                    ti.InstanceId = message.FromTaskInstanceId;
                }
                OnTransitionStatusChanged(ti.InstanceId);
            }
            else
            {
                log.Debug("Ignoring message: {0}", message);
            }
            
        }

        #endregion

        

        #region IMessageConsumer<TaskSelected> Members

        private void Handle(TaskSelected message)
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

        private void Handle(TaskCompleted message)
        {
            RequireActivation(true);
            if (message.ParentTaskInstanceId != this.InstanceId) throw new Exception();
            if (message is MultiTaskCompleted) throw new Exception();
            TransitionInfo ti = GetTransition(message.FromTaskInstanceId);
            if (ti == null) throw new Exception();
            log.Info("MT Child task {0} has completed", ti.InstanceId);
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

        #endregion

        #region IMessageConsumer<TaskCancelled> Members

        private void Handle(TaskCancelled message)
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

        private void Handle(TaskFailed message)
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

        public override void HandleTaskExecEvent(TaskExecEvent ev)
        {
            base.HandleTaskExecEvent(ev);
            if (ev is TaskEnabled)
            {
                Handle((TaskEnabled)ev);
            }
            else if (ev is TaskSelected)
            {
                Handle((TaskSelected)ev);
            }
            else if (ev is TaskCompleted)
            {
                Handle((TaskCompleted)ev);
            }
            else if (ev is TaskFailed)
            {
                Handle((TaskFailed)ev);
            }
            else if (ev is TaskCancelled)
            {
                Handle((TaskCancelled)ev);
            }
            else throw new Exception();
        }
        
    }
}
