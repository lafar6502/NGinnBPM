using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    public class TaskControlCommand : ProcessMessage
    {
        public string ToTaskInstanceId { get; set; }
    }

    public class EnableChildTask : TaskControlCommand
    {
        public string ProcessDefinitionId { get; set; }
        public string TaskId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
        /// <summary>
        /// Used for multi-instance tasks
        /// </summary>
        public List<Dictionary<string, object>> MultiInputData { get; set; }
    }

    public class StartProcess : TaskControlCommand
    {
        public string ProcessDefinitionId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
    }

    public class CancelTask : TaskControlCommand
    {
        public string Reason { get; set; }
    }




    

}
