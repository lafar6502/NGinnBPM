using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime
{
    public abstract class TaskInstance
    {
        public string InstanceId { get; set; }
        public string ParentTaskInstanceId { get; set; }
        public string ProcessInstanceId { get; set; }
        public string TaskId { get; set; }
        public string ProcessDefinitionId { get; set; }

        /// <summary>
        /// Current task data
        /// </summary>
        public Dictionary<string, object> TaskData { get; set; }

        protected ITaskExecutionContext Context { get; set; }

        public virtual void Activate(ITaskExecutionContext ctx)
        {
            Context = ctx;
        }

        public virtual void Deactivate()
        {

        }

        public virtual void Enable(Dictionary<string, object> inputData)
        {
            
        }

        public virtual void ForceComplete(Dictionary<string, object> updatedData)
        {

        }

        public virtual void Cancel(string reason)
        {

        }

        public virtual void ForceFail(string errorInfo)
        {
        }

        public virtual void Select()
        {

        }


    }
}
