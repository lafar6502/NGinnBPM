using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    /// <summary>
    /// Inter-task message is sent by 'SendMessageTaskInstance'
    /// and received by 'AwaitMessageTaskInstance'.
    /// </summary>
    public class InterTaskMessage : TaskExecEvent
    {
        /// <summary>
        /// Message recipient ID (a correlation id)
        /// </summary>
        public string ToMailbox { get; set; }
        /// <summary>
        /// Event data.
        /// </summary>
        public Dictionary<string, object> Data { get; set; }
    }
}
