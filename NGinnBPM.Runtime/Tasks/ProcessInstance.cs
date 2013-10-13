using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.Lib.Data;
using NGinnBPM.Runtime.Events;
using System.Runtime.Serialization;

namespace NGinnBPM.Runtime
{

    /// <summary>
    /// Represents a process instance.
    /// </summary>
    [DataContract]
    [Serializable]
    public class ProcessInstance : CompositeTaskInstance
    {
        /// <summary>
        /// Process correlation id represents an external identifier
        /// assigned to a process instance
        /// </summary>
        [DataMember(IsRequired=false)]
        [TaskParameter(IsInput=true, DynamicAllowed=true, Required=false)]
        public string ProcessCorrelationId { get; set; }

        /// <summary>
        /// Id of user who started the process
        /// </summary>
        [DataMember(IsRequired = false)]
        [TaskParameter(IsInput = true, DynamicAllowed = true, Required = false)]
        public string StartedBy { get; set; }

        /// <summary>
        /// Message bus endpoint
        /// that should be notified about process completion
        /// by ProcessCompletedEvent
        /// This endpoint will be set when process is started
        /// by StartProcessMessage or StartSubprocessMessage
        /// TODO: implement that
        /// </summary>
        [DataMember(IsRequired=false)]
        [TaskParameter(IsInput=true, DynamicAllowed=true, Required=false)]
        public string NotifyEndpoint { get; set; }

        public override void Enable(Dictionary<string, object> inputData)
        {
            this.ProcessStartDate = DateTime.Now;
            base.Enable(inputData);
            
            ProcessStartedEvent pse = new ProcessStartedEvent(InstanceId, this.ParentTaskInstanceId);
            Context.MessageBus.Notify(pse);
        }
    }
}
