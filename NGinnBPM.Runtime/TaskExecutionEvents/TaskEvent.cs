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
    public class TaskExecEvent : ProcessMessage
    {

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

    /// <summary>
    /// This message will be reported if a task fails and there's an error handler anywhere up to the process root
    /// task.
    /// </summary>
    public class TaskFailed : TaskExecEvent
    {
        public string ErrorInfo { get; set; }
        /// <summary>
        /// true if error is 'controlled'
        /// </summary>
        public bool IsExpected { get; set; }
    }

    public class TaskCancelled : TaskExecEvent
    {
    }


}
