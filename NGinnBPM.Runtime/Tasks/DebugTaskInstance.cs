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
    [DataContract]
    public class DebugTaskInstance : AtomicTaskInstance
    {
        [DataMember]
        public bool DoFail { get; set; }

        protected override void OnTaskEnabled()
        {
            if (DoFail)
            {
                this.ForceFail("testing the failure");
            }
        }
        
    }
}
