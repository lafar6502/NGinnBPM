using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel.Exceptions
{
    /// <summary>
    /// Undefined process data type
    /// </summary>
    [Serializable]
    class DataTypeUndefinedException : ProcessDefinitionException
    {
        public DataTypeUndefinedException()
        {
        }

        public DataTypeUndefinedException(string typeName)
            : base("Data type undefined: " + typeName)
        {
        }

        protected DataTypeUndefinedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
