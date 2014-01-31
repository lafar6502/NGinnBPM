using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Task instance for debugging purposes. Must be manually completed and logs all activity.
    /// </summary>
    [DataContract]
    public class DebugTaskInstance : AtomicTaskInstance
    {
        [DataMember]
        public bool DoFail { get; set; }
        [DataMember]
        public bool Delay { get; set; }
        protected override void OnTaskEnabling()
        {
            if (Delay)
            {
                Context.ScheduleTaskEvent(new TaskTimerEvent
                {
                    ParentTaskInstanceId = this.InstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    FromProcessInstanceId = this.ProcessInstanceId,
                    Mode = MessageHandlingMode.AnotherTransaction
                }, DateTime.Now.AddSeconds(10));
            }
            else
            {
                if (DoFail)
                {
                    this.ForceFail("testing the failure");
                }
                else
                {
                    Complete();
                }
            }
        }


        public override void HandleTaskExecEvent(TaskExecutionEvents.TaskExecEvent ev)
        {
            if (Status != TaskStatus.Enabled && Status != TaskStatus.Selected &&
                Status != TaskStatus.Enabling) return;
            if (ev is TaskTimerEvent)
            {
                if (DoFail)
                {
                    ForceFail("FAIL");
                }
                else
                {
                    Complete();
                }
            }
        }
        
    }
}
