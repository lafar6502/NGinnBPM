using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.ProcessModel.Data;

namespace NGinnBPM.ProcessModel
{
    [DataContract]
    public abstract class TaskDef : NodeDef
    {
        public TaskDef()
        {
            
        }

        [DataMember]
        public TaskSplitType SplitType { get; set; }
        [DataMember]
        public TaskSplitType JoinType { get; set; }
        [DataMember]
        public List<VariableDef> Variables { get; set; }
        [DataMember]
        public List<DataBindingDef> InputDataBindings { get; set; }
        [DataMember]
        public List<DataBindingDef> OutputDataBindings { get; set; }
        [DataMember]
        public List<DataBindingDef> InputParameterBindings { get; set; }
        [DataMember]
        public List<DataBindingDef> OutputParameterBindings { get; set; }
        [DataMember]
        public bool IsMultiInstance { get; set; }
        [DataMember]
        public bool AutoBindVariables { get; set; }
        [DataMember]
        public string MultiInstanceSplitExpression { get; set; }
        [DataMember]
        public string MultiInstanceItemAlias { get; set; }
        [DataMember]
        public string MultiInstanceResultsBinding { get; set; }
        [DataMember]
        public List<string> OrJoinCheckList { get; set; }
        [DataMember]
        public string BeforeEnableScript { get; set; }
        [DataMember]
        public string AfterEnableScript { get; set; }
        [DataMember]
        public string BeforeCompleteScript { get; set; }

        public void AddInputDataBinding(DataBindingDef b)
        {
            if (InputDataBindings == null) InputDataBindings = new List<DataBindingDef>();
            if (InputDataBindings.Any(x => x.Target == b.Target)) throw new Exception("Binding already defined for " + b.Target);
            InputDataBindings.Add(b);
        }

        public void AddOutputDataBinding(DataBindingDef b)
        {
            if (OutputDataBindings == null) OutputDataBindings = new List<DataBindingDef>();
            if (OutputDataBindings.Any(x => x.Target == b.Target)) throw new Exception("Binding already defined for " + b.Target);
            OutputDataBindings.Add(b);
        }
    }
}
