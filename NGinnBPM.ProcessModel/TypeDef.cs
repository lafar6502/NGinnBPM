using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract]
    public abstract class TypeDef
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public List<KeyValue> ExtensionProperties { get; set; }
    }
}
