using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.TaskExecutionEvents
{
    /// <summary>
    /// Task execution events, delivered synchronously in a task execution context
    /// to parent task instances
    /// </summary>
    public class TaskExecEvent
    {
        public string InstanceId { get; set; }
        public string ParentTaskInstanceId { get; set; }
    }

    /// <summary>
    /// Notifies parent that a task has been enabled
    /// Can be a result of EnableTask control message 
    /// </summary>
    public class TaskEnabled : TaskExecEvent
    {
    }

    public class TaskSelected : TaskExecEvent
    {
    }

    public class TaskCompleted : TaskExecEvent
    {
        public Dictionary<string, object> OutputData { get; set; }
    }

    public class TaskFailed : TaskExecEvent
    {
    }

    public class TaskCancelled : TaskExecEvent
    {
    }


}
