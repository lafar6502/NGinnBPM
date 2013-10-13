using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Tasks
{
    /// <summary>
    /// Atomic task is, well, atomic - it doesn't contain other tasks.
    /// </summary>
    public abstract class AtomicTaskInstance : TaskInstance
    {
    }
}
