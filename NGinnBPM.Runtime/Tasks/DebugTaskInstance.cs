using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NLog;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Task instance for debugging purposes. Must be manually completed and logs all activity.
    /// </summary>
    [DataContract]
    public class DebugTaskInstance : AtomicTaskInstance
    {
        private static Logger log = LogManager.GetCurrentClassLogger();

        [DataMember]
        public bool DoFail { get; set; }
        [DataMember]
        public bool Delay { get; set; }
        
        private void DumpState()
        {
            log.Info("Debug Task {0} [{1}]. Status: {2}", TaskId, InstanceId, Status);
            foreach (var k in TaskData.Keys)
            {
                log.Info("  {0}={1}", k, TaskData[k]);
            }
        }

        protected override void OnTaskEnabling()
        {
            DumpState();
            if (Delay)
            {
                Context.ScheduleTaskEvent(new TaskTimerEvent
                {
                    ParentTaskInstanceId = this.InstanceId,
                    FromTaskInstanceId = this.InstanceId,
                    FromProcessInstanceId = this.ProcessInstanceId
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
