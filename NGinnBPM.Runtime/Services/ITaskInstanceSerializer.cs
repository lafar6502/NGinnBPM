using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime.Services
{
    public interface ITaskInstanceSerializer
    {
        string Serialize(TaskInstance ti, out string taskTypeId);
        TaskInstance Deserialize(string data, string taskTypeId);
    }
}
