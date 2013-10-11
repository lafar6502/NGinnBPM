using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Package")]
    public class PackageDef : IValidate
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<ProcessDef> ProcessDefinitions { get; set; }
        [DataMember]
        public List<TypeSetDef> PackageTypeSets { get; set; }
        [DataMember]
        public List<string> ExternalResources { get; set; }
        [DataMember]
        public List<KeyValue> ExtensionProperties { get; set; }

        #region IValidate Members

        public bool Validate(List<string> problemsFound)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
