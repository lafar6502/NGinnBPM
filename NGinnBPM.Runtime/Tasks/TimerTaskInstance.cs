using System;
using System.Collections.Generic;
using NGinnBPM.MessageBus;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.ProcessModel;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.Runtime;


namespace NGinnBPM.Runtime.Tasks
{
    [Serializable]
    [DataContract]
    public class TimerTaskInstance : AtomicTaskInstance
    {
        
        [DataMember]
        public TimeSpan Delay { get;set;}

        [IgnoreDataMember]
        public DateTime ExpirationDate
        {
            get { return DateTime.Now + Delay; }
            set { Delay = ExpirationDate - DateTime.Now; }
        }

        protected override void OnTaskEnabling()
        {
            base.OnTaskEnabling();
            if (Delay.TotalSeconds <= 1)
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
                }, DateTime.Now + this.Delay);
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
                    if (DateTime.Now + this.Delay <= DateTime.Now)
                    {
                        Complete();
                    }
                }
            }
        }

        #endregion
    }
}
