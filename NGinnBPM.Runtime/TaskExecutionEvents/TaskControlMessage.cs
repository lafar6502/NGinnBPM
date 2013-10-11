using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    public class TaskControlMessage
    {
        public string FromProcessInstanceId { get; set; }
        public string FromTaskInstanceId { get; set; }
        public string ToTaskInstanceId { get; set; }
        public string CorrelationId { get; set; }
    }

    public class EnableChildTask : TaskControlMessage
    {
        public Dictionary<string, object> InputData { get; set; }
    }

    public class StartProcess : TaskControlMessage
    {
        public string ProcessDefinitionId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
    }





    

}
