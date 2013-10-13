using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="CompositeTask")]
    public class CompositeTaskDef : TaskDef
    {
        public CompositeTaskDef()
        {
            Tasks = new List<TaskDef>();
            Places = new List<PlaceDef>();
            Flows = new List<FlowDef>();
        }

        [DataMember]
        public List<TaskDef> Tasks { get; set; }
        [DataMember]
        public List<PlaceDef> Places { get; set; }
        [DataMember]
        public List<FlowDef> Flows { get; set; }

        public override bool Validate(List<string> problemsFound)
        {
            return true;
        }

        public TaskDef FindTask(string id)
        {
            foreach (var t in Tasks)
            {
                if (t.Id == id) return t;
                if (t is CompositeTaskDef)
                {
                    var t2 = ((CompositeTaskDef)t).FindTask(id);
                    if (t2 != null) return t2;
                }
            }
            return null;
        }
    }
}
