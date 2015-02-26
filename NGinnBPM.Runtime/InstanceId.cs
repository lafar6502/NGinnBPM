using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGinnBPM.Runtime
{
    public class InstanceId
    {
        public static string GetProcessInstanceId(string taskInstanceId)
        {
            var idx = taskInstanceId.IndexOf('.');
            if (idx < 0) return taskInstanceId;
            return taskInstanceId.Substring(0, idx);
        }

        public static bool IsSameProcessInstance(string taskInstance1, string taskInstance2)
        {
            return GetProcessInstanceId(taskInstance1) == GetProcessInstanceId(taskInstance2);
        }

        public static string GetParentTaskInstanceId(string taskInstanceId)
        {
            var idx = taskInstanceId.LastIndexOf('.');
            if (idx < 0) return taskInstanceId;
            return taskInstanceId.Substring(0, idx);
        }


    }
}
