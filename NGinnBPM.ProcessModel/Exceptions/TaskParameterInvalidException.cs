using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class TaskParameterInvalidException : TaskRuntimeException
    {
        public TaskParameterInvalidException()
        {
        }

        protected TaskParameterInvalidException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public TaskParameterInvalidException(string paramName, string message)
            : base("Task input parameter missing or invalid: " + paramName)
        {
        }
    }
}
