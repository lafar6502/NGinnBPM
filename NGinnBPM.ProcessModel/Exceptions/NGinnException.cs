using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class NGinnException : Exception
    {
        private bool _noRetry = false;
        private string _taskId;
        private string _processDef;

        public NGinnException(string msg)
            : base(msg)
        {
        }

        public NGinnException()
        {
        }


        protected NGinnException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


        public override string Message
        {
            get
            {
                return string.Format("Error in process {0} (task {1}): {2}", ProcessDefinitionId, TaskId, base.Message);
            }
        }

        public NGinnException SetPermanent(bool perm) { _noRetry = perm; return this; }
        public NGinnException SetTaskId(string t) { _taskId = t; return this; }
        public NGinnException SetProcessDef(string t) { _processDef = t; return this; }
        public NGinnException SetTaskAndProcessDef(string processDef, string taskId) { _processDef = processDef; _taskId = taskId; return this; }

        public string TaskId
        {
            get { return _taskId; }
            set { _taskId = value; }
        }

        public string ProcessDefinitionId
        {
            get { return _processDef; }
            set { _processDef = value; }
        }

        public bool Permanent
        {
            get { return _noRetry; }
            set { _noRetry = value; }
        }
    }
}
