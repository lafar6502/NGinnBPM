using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    /// <summary>
    /// Starts a sub-process
    /// Message used internally by NGinn.BPM 
    /// </summary>
    public class StartSubProcess : ProcessMessage
    {
        public string DefinitionId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
    }
}
