using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class TaskDataInvalidException : TaskRuntimeException
    {
        public TaskDataInvalidException()
        {

        }

        protected TaskDataInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public TaskDataInvalidException(string taskId, string instanceId, string message)
            : base(string.Format("Invalid task data in task {0}: {1}", taskId, message))
        {
            SetInstanceId(instanceId).SetTaskId(taskId);
        }

        public TaskDataInvalidException(string message)
            : base(message)
        {
        }
    }
}
