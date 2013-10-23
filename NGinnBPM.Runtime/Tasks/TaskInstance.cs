using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using NGinnBPM.ProcessModel.Util;

namespace NGinnBPM.Runtime.Tasks
{
    [DataContract]
    public abstract class TaskInstance
    {
        [DataMember]
        public virtual string InstanceId { get; set; }
        [DataMember]
        public string ParentTaskInstanceId { get; set; }
        [DataMember]
        public string ProcessInstanceId { get; set; }
        [DataMember]
        public string TaskId { get; set; }
        [DataMember]
        public string ProcessDefinitionId { get; set; }
        [DataMember]
        public TaskStatus Status { get; set; }
        /// <summary>
        /// Additional information in case of failure or cancel
        /// </summary>
        [DataMember]
        public string StatusInfo { get; set; }
        /// <summary>
        /// Current task data
        /// </summary>
        [DataMember]
        [JsonConverter(typeof(TaskDataJsonConverter))]
        public Dictionary<string, object> TaskData { get; set; }
        [DataMember]
        public DateTime EnabledDate { get; set; }
        [DataMember]
        public DateTime? FinishedDate { get; set; }

        [IgnoreDataMember]
        protected ITaskExecutionContext Context { get; set; }
        [IgnoreDataMember]
        protected ProcessDef ProcessDefinition { get; set; }
        [IgnoreDataMember]
        protected IProcessScriptRuntime ScriptRuntime { get; set; }
        
        [IgnoreDataMember]
        protected TaskDef TaskDefinition { get; set; }
        
        public TaskInstance()
        {
            Status = TaskStatus.Enabling;
        }

        public virtual void Activate(ITaskExecutionContext ctx, ProcessDef processDef, IProcessScriptRuntime scriptRuntime)
        {
            Context = ctx;
            ProcessDefinition = processDef;
            TaskDefinition = processDef.GetRequiredTask(this.TaskId);
            ScriptRuntime = scriptRuntime;
            if (TaskDefinition == null) throw new Exception("Task not found in process definition: " + this.TaskId);
        }

        protected void RequireActivation(bool b)
        {
            if (this.Context == null) throw new Exception("Task not activated");
        }

        public virtual void Deactivate()
        {

        }

        [IgnoreDataMember]
        public virtual bool IsAlive
        {
            get
            {
                return Status == TaskStatus.Enabled || Status == TaskStatus.Cancelling ||
                    Status == TaskStatus.Selected || Status == TaskStatus.Enabling;
            }
        }

        /// <summary>
        /// Enable task.
        /// </summary>
        /// <param name="inputData"></param>
        public virtual void Enable(Dictionary<string, object> inputData)
        {
            if (Status != TaskStatus.Enabling) throw new Exception("Invalid status!");
            this.TaskData = new Dictionary<string, object>();
            ScriptRuntime.InitializeNewTask(this, inputData, Context);
            this.Status = TaskStatus.Enabled;
            EnabledDate = DateTime.Now;
            this.OnTaskEnabled();
            if (this.Status == TaskStatus.Enabled)
            {
                Context.NotifyTaskEvent(new TaskEnabled { FromTaskInstanceId = this.InstanceId, ParentTaskInstanceId = this.ParentTaskInstanceId });
            }
        }

        /// <summary>
        /// Override this to execute some custom 'enable' logic.
        /// </summary>
        protected virtual void OnTaskEnabled()
        {
        }

        public virtual void ForceComplete(Dictionary<string, object> updatedData)
        {

        }

        public virtual void Cancel(string reason)
        {
            DefaultHandleTaskCancel(reason);
        }

        public virtual void ForceFail(string errorInfo)
        {
            DefaultHandleTaskFailure(errorInfo, true);
        }

        public virtual void Select()
        {

        }

        protected void Complete()
        {
            DefaultHandleTaskCompletion(null);
        }


        /// <summary>
        /// Return task output data.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, object> GetOutputData()
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (TaskDefinition.Variables == null) return ret;
            foreach (var vd in TaskDefinition.Variables)
            {
                if (vd.VariableDir == ProcessModel.Data.VariableDef.Dir.Out ||
                    vd.VariableDir == ProcessModel.Data.VariableDef.Dir.InOut)
                {
                    ret[vd.Name] = TaskData[vd.Name];
                }
            }
            return ret;
        }

        protected virtual void DefaultHandleTaskCompletion(Dictionary<string, object> updateData)
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected) throw new Exception("Invalid task status");
            if (updateData != null)
            {
                foreach (string gk in updateData.Keys)
                {
                    TaskData[gk] = updateData[gk];
                }
            }
            Status = TaskStatus.Completed;
            Context.NotifyTaskEvent(new TaskCompleted
            {
                FromTaskInstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId,
                OutputData = this.GetOutputData()
            });
        }

        /// <summary>
        /// Default cancellation handler.Override it to add custom cancellation logic.
        /// </summary>
        /// <param name="reason"></param>
        protected virtual void DefaultHandleTaskCancel(string reason)
        {
            Status = TaskStatus.Cancelled;
            StatusInfo = reason;
            Context.NotifyTaskEvent(new TaskCancelled
            {
                FromTaskInstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId,
                CorrelationId = null
            });
        }

        /// <summary>
        /// Default task failure handler. Override it to provide custom logic.
        /// </summary>
        /// <param name="errorInfo"></param>
        /// <param name="failureIntended"></param>
        protected virtual void DefaultHandleTaskFailure(string errorInfo, bool failureIntended)
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Enabling &&
                Status != TaskStatus.Selected && Status != TaskStatus.Cancelling)
                throw new Exception("Invalid status");
            Status = TaskStatus.Failed;
            StatusInfo = errorInfo;
            Context.NotifyTaskEvent(new TaskFailed
            {
                FromTaskInstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId,
                IsExpected = failureIntended,
                ErrorInfo = errorInfo
            });
        }

        public virtual void HandleTaskExecEvent(TaskExecEvent ev)
        {
            if (ev.ParentTaskInstanceId != this.InstanceId) throw new Exception("Invalid ParentTaskInstanceId");
        }

    }
}
