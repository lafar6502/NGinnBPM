using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    /// <summary>
    /// Process definition errors - static errors in process and task definitions.
    /// </summary>
    [Serializable]
    public class ProcessDefinitionException : NGinnException
    {
        public ProcessDefinitionException()
        {
        }

        public ProcessDefinitionException(string msg)
            : base(msg)
        {
            SetPermanent(true);
        }

        public ProcessDefinitionException(string processDefinition, string taskId, string msg)
            : base(msg)
        {
            SetTaskId(taskId).SetProcessDef(processDefinition).SetPermanent(true);
        }

        protected ProcessDefinitionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
