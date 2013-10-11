using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    /// <summary>
    /// Thrown when process node is not found
    /// </summary>
    [Serializable]
    public class UndefinedNetNodeException : ProcessDefinitionException
    {
        public UndefinedNetNodeException(string definitionId, string nodeId) :
            base(string.Format("Undefined node in process {0}: {1}", definitionId, nodeId))
        {
            SetProcessDef(definitionId).SetTaskId(nodeId).SetPermanent(true);
        }

        public UndefinedNetNodeException()
        {
        }

        protected UndefinedNetNodeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public UndefinedNetNodeException(string msg)
            : base(msg)
        {
            SetPermanent(true);
        }
    }
}
