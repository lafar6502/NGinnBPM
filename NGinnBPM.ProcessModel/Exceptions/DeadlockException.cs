using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel.Exceptions
{
    [Serializable]
    public class DeadlockException : TaskRuntimeException
    {
        public DeadlockException()
        {
        }

        public DeadlockException(string taskInstance, string taskId, string definitionId) :
            base(string.Format("Deadlock in task instance {0} (task id {1}, process {2})", taskInstance, taskId, definitionId))
        {
            SetInstanceId(taskInstance).SetTaskId(taskId);
        }

    }
}
