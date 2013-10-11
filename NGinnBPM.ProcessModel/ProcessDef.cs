using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using NGinnBPM.ProcessModel.Data;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Process")]
    public class ProcessDef : IValidate
    {
        [DataMember]
        public string ProcessName { get; set; }
        
        [DataMember]
        public int Version { get; set; }
        
        [DataMember]
        public string PackageName { get; set; }
        
        [DataMember]
        public TypeSet DataTypes { get; set; }

        [DataMember]
        public CompositeTaskDef Body { get; set; }

        [DataMember]
        public List<KeyValue> ExtensionProperties { get; set; }
        
        public string DefinitionId
        {
            get { return string.Format("{0}.{1}.{2}", PackageName, ProcessName, Version); }
        }

        #region IValidate Members

        public bool Validate(List<string> problemsFound)
        {
            if (Body == null)
            {
                problemsFound.Add("Error: process body not defined");
                return false;
            }
            return this.Body.Validate(problemsFound);
        }

        #endregion
    }
}
