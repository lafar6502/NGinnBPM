using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class ProcessScriptCompilationError : ProcessDefinitionException
    {
        public ProcessScriptCompilationError()
        {
        }

        public ProcessScriptCompilationError(string msg)
            : base(msg)
        {
        }

        protected ProcessScriptCompilationError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
