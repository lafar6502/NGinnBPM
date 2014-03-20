using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.ProcessModel.Data;

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
        public List<TypeSet> PackageTypeSets { get; set; }
        [DataMember]
        public List<string> ExternalResources { get; set; }
        

        #region IValidate Members

        public bool Validate(List<string> problemsFound)
        {
            return true;
        }

        #endregion
    }
}
