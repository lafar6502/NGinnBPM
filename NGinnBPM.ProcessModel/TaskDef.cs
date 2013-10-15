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

        public IEnumerable<FlowDef> GetFlowsForPortOut(TaskOutPortType portType)
        {
            return this.FlowsOut.Where(x => x.SourcePortType == portType);
        }

        /// <summary>
        /// Get task input data definition
        /// </summary>
        /// <returns></returns>
        public virtual StructDef GetInputDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in Variables)
            {
                if (vd.VariableDir == VariableDef.Dir.In || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
            }
            return sd;
        }

        /// <summary>
        /// Get task output data definition
        /// </summary>
        /// <returns></returns>
        public virtual StructDef GetOutputDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in Variables)
            {
                if (vd.VariableDir == VariableDef.Dir.Out || vd.VariableDir == VariableDef.Dir.InOut)
                {
                    sd.Members.Add(vd);
                }
            }
            return sd;
        }

        /// <summary>
        /// Get the definition of internal task data structure (all variables)
        /// Warning: there are no required fields in the internal data schema. So any variable, even the required ones, can be skipped
        /// in an xml document.
        /// </summary>
        /// <returns></returns>
        public virtual StructDef GetInternalDataSchema()
        {
            if (ParentProcess == null) throw new Exception();
            StructDef sd = new StructDef();
            sd.ParentTypeSet = ParentProcess.DataTypes;
            foreach (VariableDef vd in Variables)
            {
                VariableDef vd2 = new VariableDef(vd); vd2.IsRequired = false;
                sd.Members.Add(vd2);
            }
            return sd;
        }
    }
}
