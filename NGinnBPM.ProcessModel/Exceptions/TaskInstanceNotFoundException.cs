using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class TaskInstanceNotFoundException : TaskRuntimeException
    {
        public TaskInstanceNotFoundException()
        {
        }

        protected TaskInstanceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public TaskInstanceNotFoundException(string instanceId)
            : base("Task instance not found: " + instanceId)
        {
            SetInstanceId(instanceId);
        }
    }
}
