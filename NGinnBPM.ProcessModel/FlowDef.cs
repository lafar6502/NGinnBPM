using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Flow")]
    public class FlowDef
    {
        [DataMember]
        public string From { get; set; }
        [DataMember]
        public string To { get; set; }
        [DataMember]
        public string Label { get; set; }
        [DataMember]
        public bool IsCancelling { get; set; }
        [DataMember]
        public string InputCondition { get; set; }
        [DataMember]
        public List<KeyValue> ExtensionProperties { get; set; }
        [DataMember]
        public int EvalOrder { get; set; }
        [DataMember]
        public TaskInPortType TargetPortType { get; set; }
        [DataMember]
        public TaskOutPortType SourcePortType { get; set; }
    }
}
