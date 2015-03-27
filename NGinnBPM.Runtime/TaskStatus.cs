using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// Task instance status
    /// </summary>
    public enum TaskStatus
    {
        Enabling = 0,
        Enabled = 1,
        Selected = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5,
        Cancelling = 6 
    }

    
}
