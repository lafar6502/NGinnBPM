using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    public enum MessageHandlingMode
    {
        SameTransaction,
        AnotherTransaction
    }

    /// <summary>
    /// base class for all inter-task messages coordinating process execution
    /// </summary>
    public class ProcessMessage
    {
        
        public string CorrelationId { get; set; }
        public string FromTaskInstanceId { get; set; }
        public string FromProcessInstanceId { get; set; }

        
    }
}
