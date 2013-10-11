using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="AtomicTask")]
    public class AtomicTaskDef : TaskDef
    {
        [DataMember]
        public NGinnTaskType TaskType { get; set; }
        [DataMember]
        public string CustomType { get; set; }

        public override bool Validate(List<string> problemsFound)
        {
            List<string> problems = new List<string>();
            if (TaskType == NGinnTaskType.Custom && string.IsNullOrEmpty(CustomType))
            {
                problems.Add(string.Format("Error in task {0}: CustomType is empty", Id));
            }
            if (problemsFound != null)
                problemsFound.AddRange(problems);
            return problems.Count == 0;
        }
    }
}
