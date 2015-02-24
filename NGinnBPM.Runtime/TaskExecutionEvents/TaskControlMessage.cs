using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    /// <summary>
    /// Task control commands 
    /// </summary>
    public class TaskControlCommand : ProcessMessage
    {
        /// <summary>
        /// Destination task instance
        /// </summary>
        public string ToTaskInstanceId { get; set; }
    }

    /// <summary>
    /// Enable child task of ToTaskInstanceId
    /// Sent always to composite task instance
    /// </summary>
    public class EnableChildTask : TaskControlCommand
    {
        public string ProcessDefinitionId { get; set; }
        public string TaskId { get; set; }
        public Dictionary<string, object> InputData { get; set; }
        
    }

    public class EnableMultiChildTask : EnableChildTask
    {
        /// <summary>
        /// Used for multi-instance tasks
        /// </summary>
        public List<Dictionary<string, object>> MultiInputData { get; set; }
    }

  

    public class CancelTask : TaskControlCommand
    {
        public string Reason { get; set; }
    }

    public class SelectTask : TaskControlCommand
    {
    }

    public class FailTask : TaskControlCommand
    {
        public string ErrorInfo { get; set; }
    }

    public class ForceCompleteTask : TaskControlCommand
    {
        public Dictionary<string, object> UpdateData { get; set; }
    }




    

}
