using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.ProcessModel.Data;

namespace NGinnBPM.ProcessModel
{
    [DataContract]
    public abstract class NodeDef : IValidate, IHaveMetadata
    {
        [DataMember]
        public string Id { get; set; }
        [DataMember]
        public string Label { get; set; }
        [DataMember]
        public string Description { get; set; }
        
        
        [IgnoreDataMember]
        public CompositeTaskDef Parent { get; set; }
        [IgnoreDataMember]
        public ProcessDef ParentProcess { get; set; }

        public abstract bool Validate(List<string> problemsFound);


        #region IHaveExtensionProperties Members


        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<string, Dictionary<string, string>> ExtensionProperties { get; set; }
        
        public string GetMetaValue(string xmlns, string name)
        {
            return ExtensionPropertyHelper.GetExtensionProperty(ExtensionProperties, xmlns, name);
        }

        public void SetMetaValue(string xmlns, string name, string value)
        {
            if (ExtensionProperties == null) ExtensionProperties = new Dictionary<string, Dictionary<string, string>>();
            ExtensionPropertyHelper.SetExtensionProperty(ExtensionProperties, xmlns, name, value);
        }

        public Dictionary<string, string> GetMetadata(string ns)
        {
            return ExtensionPropertyHelper.GetExtensionProperties(ExtensionProperties, ns);
        }

        #endregion

        [IgnoreDataMember]
        public IEnumerable<FlowDef> FlowsOut
        {
            get
            {
                if (Parent == null) throw new Exception();
                return Parent.Flows.Where(x => x.From == this.Id);
            }
        }

        [IgnoreDataMember]
        public IEnumerable<FlowDef> FlowsIn
        {
            get
            {
                if (Parent == null) throw new Exception();
                return Parent.Flows.Where(x => x.To == this.Id);
            }
        }
        [IgnoreDataMember]
        public IEnumerable<NodeDef> NodesOut
        {
            get
            {
                return this.FlowsOut.Select(x => Parent.GetNode(x.To));
            }
        }

        [IgnoreDataMember]
        public IEnumerable<NodeDef> NodesIn
        {
            get
            {
                return this.FlowsIn.Select(x => Parent.GetNode(x.From));
            }
        }


        
    }
}
