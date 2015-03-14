using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.Runtime
{
    public class CompositeTaskInstanceInfo
    {
        public string InstanceId { get; set; }
        public string ProcessInstanceId { get; set; }
        public string TaskId { get; set; }
        public string ProcessDefinitionId { get; set; }
        public List<string> Marking { get; set; }
        public List<string> ActiveTasks { get; set; }
        public TaskStatus Status { get; set; }
    }

    public class ProcessInstanceInfo : CompositeTaskInstanceInfo
    {
    }
}
