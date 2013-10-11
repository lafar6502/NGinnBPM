using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class TaskRuntimeException : NGinnException
    {
        private string _instanceId;
        private string _processInstanceId;
        public TaskRuntimeException SetInstanceId(string t) { _instanceId = t; return this; }
        public TaskRuntimeException SetProcessInstance(string t) { _processInstanceId = t; return this; }

        public TaskRuntimeException()
        {
        }

        protected TaskRuntimeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public TaskRuntimeException(string msg)
            : base(msg)
        {
        }
    }
}
