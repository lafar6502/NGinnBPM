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

        public void AddTask(TaskDef td)
        {
            td.Parent = this;
            Tasks.Add(td);
        }

        public void AddPlace(PlaceDef pd)
        {
            pd.Parent = this;
            Places.Add(pd);
        }

        public void AddFlow(FlowDef t)
        {
            if (string.IsNullOrEmpty(t.From) || string.IsNullOrEmpty(t.To)) throw new Exception("Flow must have start and target node");
            NodeDef p = GetNode(t.From);
            if (p == null) throw new Exception("Node not defined: " + t.From);
            NodeDef q = GetNode(t.To);
            if (q == null) throw new Exception("Node not defined: " + t.To);
            t.Parent = this;
            
            if (t.IsCancelling || t.SourcePortType != TaskOutPortType.Default)
            {
                if (t.InputCondition != null && t.InputCondition.Length > 0)
                    throw new NGinnBPM.ProcessModel.Exceptions.ProcessDefinitionException(this.ParentProcess.DefinitionId, t.From, "InputCondition not allowed");
            }

            if (p is PlaceDef && q is PlaceDef) throw new Exception("Flow cannot connect two places");
            if (p is TaskDef && q is TaskDef)
            {
                //adding implicit place between p and q
                TaskDef tq = q as TaskDef;
                TaskDef tp = p as TaskDef;
                PlaceDef ptran = new PlaceDef {Id = string.Format("{0}.-.{1}", tp.Id, tq.Id),
                    Implicit = true
                };
                AddPlace(ptran);
                t.To = ptran.Id;
                FlowDef f2 = new FlowDef();
                f2.From = ptran.Id;
                f2.To = tq.Id;
                AddFlow(t);
                AddFlow(f2);
            }
            else
            {
                Flows.Add(t);
            }
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

        public PlaceDef FindPlace(string id)
        {
            foreach (var pl in Places)
            {
                if (pl.Id == id) return pl;
            }
            foreach (var ct in Tasks.Where(t => t is CompositeTaskDef).Cast<CompositeTaskDef>())
            {
                var pd = ct.FindPlace(id);
                if (pd != null) return pd;
            }
            return null;
        }

        public NodeDef FindNode(string id)
        {
            foreach (var pl in Places)
            {
                if (pl.Id == id) return pl;
            }
            foreach (var t in Tasks)
            {
                if (t.Id == id) return t;
            }
            foreach (var ct in Tasks.Where(t => t is CompositeTaskDef).Cast<CompositeTaskDef>())
            {
                var pd = ct.FindNode(id);
                if (pd != null) return pd;
            }
            return null;
        }

        public NodeDef GetNode(string id)
        {
            var t = Tasks.Find(x => x.Id == id);
            if (t != null) return t;
            return Places.Find(x => x.Id == id);
        }

        public TaskDef GetTask(string id)
        {
            return Tasks.FirstOrDefault(x => x.Id == id);
        }

        public PlaceDef GetPlace(string id)
        {
            return Places.FirstOrDefault(x => x.Id == id);
        }
        
        /// <summary>
        /// Fix parent references in child tasks.
        /// Invoke this after deserialization.
        /// </summary>
        public virtual void UpdateParentRefs()
        {
            foreach (var pl in this.Places)
            {
                pl.Parent = this;
                pl.ParentProcess = this.ParentProcess;
            }

            foreach (var td in this.Tasks)
            {
                td.Parent = this;
                td.ParentProcess = this.ParentProcess;
                if (td is CompositeTaskDef) ((CompositeTaskDef)td).UpdateParentRefs();
            }
            foreach (var fl in this.Flows)
            {
                fl.Parent = this;
            }
        }
    }
}
