using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    public enum VariableType
    {
        In,
        Out,
        InOut,
        Local
    }

    [DataContract(Name="Variable")]
    public class VariableDef : MemberDef
    {
        [DataMember]
        public VariableType VarType { get; set; }
        [DataMember]
        public string DefaultValueLiteral { get; set; }
        [DataMember]
        public string DefaultValueScript { get; set; }
    }
}
