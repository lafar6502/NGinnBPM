using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    public class InterTaskMessage : TaskExecEvent
    {
        public string ToMailbox { get; set; }
        public Dictionary<string, object> Data { get; set; }


    }
}
