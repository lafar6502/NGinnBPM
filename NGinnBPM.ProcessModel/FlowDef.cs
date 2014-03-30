using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.ProcessModel.Data;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Flow")]
    public class FlowDef : IHaveMetadata
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
        public int EvalOrder { get; set; }
        [DataMember]
        public TaskInPortType TargetPortType { get; set; }
        [DataMember]
        public TaskOutPortType SourcePortType { get; set; }
        [IgnoreDataMember]
        public CompositeTaskDef Parent { get; set; }



        #region IHaveExtensionProperties Members

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, object>> ExtensionProperties { get; set; }



        public object GetMetaValue(string xmlns, string name)
        {
            return ExtensionPropertyHelper.GetExtensionProperty(ExtensionProperties, xmlns, name);
        }

        public void SetMetaValue(string xmlns, string name, object value)
        {
            if (ExtensionProperties == null) ExtensionProperties = new Dictionary<string, Dictionary<string, object>>();
            ExtensionPropertyHelper.SetExtensionProperty(ExtensionProperties, xmlns, name, value);
        }

        Dictionary<string, object> IHaveMetadata.GetMetadata(string ns)
        {
            return ExtensionPropertyHelper.GetExtensionProperties(ExtensionProperties, ns);
        }
        #endregion

    }
}
