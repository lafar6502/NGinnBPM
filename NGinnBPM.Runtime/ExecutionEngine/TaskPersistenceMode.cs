using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime.ExecutionEngine
{
    public enum TaskPersistenceMode
    {
        DontPersistAnything = 0,
        PersistAliveTasksOnly = 1,
        PersistAll = 2
    }
}
