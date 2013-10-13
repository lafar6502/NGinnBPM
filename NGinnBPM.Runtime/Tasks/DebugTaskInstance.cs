using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Task instance for debugging purposes. Must be manually completed and logs all activity.
    /// </summary>
    [Serializable]
    [DataContract]
    public class DebugTaskInstance : AtomicTaskInstance
    {
        public override void Enable(Dictionary<string, object> inputData)
        {
            base.Enable(inputData);
        }

        public override void Cancel()
        {
            base.Cancel();
        }

        public override void Complete(string finishedBy, Dictionary<string, object> updatedData)
        {
            base.Complete(finishedBy, updatedData);
        }

        public override void Fail(string errorInformation)
        {
            base.Fail(errorInformation);
        }
    }
}
