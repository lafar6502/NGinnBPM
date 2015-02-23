using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.Runtime.Services;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Waits for an inter-task message.
#warning TODO : what about the message correlation registry???
    /// </summary>
    [DataContract]
    public class AwaitMessageTaskInstance : AtomicTaskInstance
    {
        [DataMember]
        public string Mailbox { get; set; }
        
        protected override void OnTaskEnabling()
        {
            //var srv = Context.GetService<IMessageCorrelationRegistry>();
            //srv.Subscribe(this.Mailbox, this.InstanceId);
        }

        public override void HandleTaskExecEvent(TaskExecEvent ev)
        {
            if (ev is InterTaskMessage)
            {
                InterTaskMessage tm = ev as InterTaskMessage;
                if (tm.ToMailbox != this.Mailbox) return;
                //var sr = Context.GetService<IMessageCorrelationRegistry>();
                //sr.Unsubscribe(this.Mailbox, this.InstanceId);
                this.Complete();
            }
            base.HandleTaskExecEvent(ev);
        }
    }
}
