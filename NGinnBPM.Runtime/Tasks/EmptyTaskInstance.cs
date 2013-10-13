using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.Runtime.Tasks
{
    [DataContract]
    public class EmptyTaskInstance : AtomicTaskInstance
    {
        public override void Enable(Dictionary<string, object> inputData)
        {
            base.Enable(inputData);
            Complete();
        }
    }
}
