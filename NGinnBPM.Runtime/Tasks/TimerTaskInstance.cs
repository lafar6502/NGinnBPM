using System;
using System.Collections.Generic;
using NGinnBPM.MessageBus;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.Runtime;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime.Tasks
{
    [Serializable]
    [DataContract]
    public class TimerTaskInstance : AtomicTaskInstance
    {
        
        [DataMember]
        public DateTime ExpirationDate { get;set;}


        protected override void OnTaskEnabling()
        {
            base.OnTaskEnabling();
            if (ExpirationDate <= DateTime.Now)
            {
                Complete();
            }
            else
            {
                Context.ScheduleTaskEvent(new TaskTimerEvent
                {
                    FromProcessInstanceId = this.ProcessInstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    ParentTaskInstanceId = this.InstanceId
                }, this.ExpirationDate);
            }
        }

        #region IMessageConsumer<TaskInstanceTimeout> Members

        public override void HandleTaskExecEvent(TaskExecEvent ev)
        {
            base.HandleTaskExecEvent(ev);
            if (ev is TaskTimerEvent)
            {
                if (this.Status == TaskStatus.Enabling || this.Status == TaskStatus.Enabled || this.Status == TaskStatus.Selected)
                {
                    if (this.ExpirationDate <= DateTime.Now)
                    {
                        Complete();
                    }
                }
            }
        }

        #endregion
    }
}
