using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using System.Runtime.Serialization;

namespace NGinnBPM.Runtime.Tasks
{
    public abstract class TaskInstance
    {
        public virtual string InstanceId { get; set; }
        public string ParentTaskInstanceId { get; set; }
        public string ProcessInstanceId { get; set; }
        public string TaskId { get; set; }
        public string ProcessDefinitionId { get; set; }
        public TaskStatus Status { get; set; }
        /// <summary>
        /// Additional information in case of failure or cancel
        /// </summary>
        public string StatusInfo { get; set; }
        /// <summary>
        /// Current task data
        /// </summary>
        public Dictionary<string, object> TaskData { get; set; }

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
            this.OnTaskEnabled();
            if (this.Status == TaskStatus.Enabled)
            {
                Context.NotifyTaskEvent(new TaskEnabled { InstanceId = this.InstanceId, ParentTaskInstanceId = this.ParentTaskInstanceId });
            }
        }

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
                InstanceId = this.InstanceId,
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
                InstanceId = this.InstanceId,
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
            Status = TaskStatus.Failed;
            StatusInfo = errorInfo;
            Context.NotifyTaskEvent(new TaskFailed
            {
                InstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId,
                IsExpected = failureIntended,
                ErrorInfo = errorInfo
            });
        }

    }
}
