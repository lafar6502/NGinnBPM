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
        

        
    }
}
