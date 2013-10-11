using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class DataValidationException : TaskRuntimeException
    {
        public DataValidationException()
        {
        }

        public DataValidationException(string msg)
            : base(msg)
        {
        }

        protected DataValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
