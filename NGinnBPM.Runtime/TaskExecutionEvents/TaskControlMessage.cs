using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    public class TaskControlMessage : ProcessMessage
    {
        public string ToTaskInstanceId { get; set; }
    }

    public class EnableChildTask : TaskControlMessage
    {
        public string ProcessDefinitionId { get; set; }
        public string TaskId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
        /// <summary>
        /// Used for multi-instance tasks
        /// </summary>
        public List<Dictionary<string, object>> MultiInputData { get; set; }
    }

    public class StartProcess : TaskControlMessage
    {
        public string ProcessDefinitionId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
    }

    public class CancelTask : TaskControlMessage
    {
        public string Reason { get; set; }
    }




    

}
