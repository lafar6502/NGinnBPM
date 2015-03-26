using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime.TaskExecutionEvents.Process
{
    public class ProcessEvent
    {
        public string InstanceId { get; set; }
        public string DefinitionId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
