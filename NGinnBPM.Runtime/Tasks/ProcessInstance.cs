using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGinnBPM.ProcessModel.Data;
using NGinnBPM.ProcessModel;
using NGinnBPM.Runtime.TaskExecutionEvents;
using System.Runtime.Serialization;

namespace NGinnBPM.Runtime.Tasks
{

    /// <summary>
    /// Represents a process instance.
    /// </summary>
    [DataContract]
    public class ProcessInstance : CompositeTaskInstance
    {
        /// <summary>
        /// Process correlation id represents an external identifier
        /// assigned to a process instance
        /// </summary>
        [DataMember(IsRequired=false)]
        //[TaskParameter(IsInput=true, DynamicAllowed=true, Required=false)]
        public string ProcessCorrelationId { get; set; }

        /// <summary>
        /// Id of user who started the process
        /// </summary>
        [DataMember(IsRequired = false)]
        //[TaskParameter(IsInput = true, DynamicAllowed = true, Required = false)]
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
        //[TaskParameter(IsInput=true, DynamicAllowed=true, Required=false)]
        public string NotifyEndpoint { get; set; }
    }
}
