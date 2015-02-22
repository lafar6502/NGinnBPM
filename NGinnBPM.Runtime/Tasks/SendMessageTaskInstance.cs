
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.Runtime.TaskExecutionEvents;
using NGinnBPM.MessageBus;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Sends an inter-task message.
    /// </summary>
    [DataContract]
    public class SendMessageTaskInstance : AtomicTaskInstance
    {
        /// <summary>
        /// Destination mailbox. 
        /// </summary>
        [DataMember]
        public string Mailbox { get; set; }
        [DataMember]
        public string Endpoint { get; set; }
        [DataMember]
        public bool WaitUntilReceived { get; set; }
        [DataMember]
        public TimeSpan WaitTimeout { get; set; }
        
        protected override void OnTaskEnabling()
        {
            var mbus = Context.GetService<IMessageBus>();
            var msg = new InterTaskMessage
            {
                FromProcessInstanceId = this.ProcessInstanceId,
                FromTaskInstanceId = this.InstanceId,
                ParentTaskInstanceId = this.ParentTaskInstanceId,
                ToMailbox = Mailbox,
                Data = this.TaskData
            };
            if (!string.IsNullOrEmpty(Endpoint))
            {
                mbus.Send(Endpoint, msg);
            }
            else
            {
                mbus.Notify(msg);
            }
        }
    }
}
