using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    /// <summary>
    /// Invalid task instance status for the operation
    /// </summary>
    [Serializable]
    public class InvalidTaskStatusException : TaskRuntimeException
    {
        public InvalidTaskStatusException()
        {
        }

        protected InvalidTaskStatusException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InvalidTaskStatusException(string instanceId, string message)
            : base(string.Format("Task {0}: {1}", instanceId, message))
        {
            SetInstanceId(instanceId);
        }

        public InvalidTaskStatusException(string msg)
            : base(msg)
        {
        }
        
    }
}
