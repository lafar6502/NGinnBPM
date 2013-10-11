using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract]
    public class MemberDef
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string MemberType { get; set; }
        [DataMember]
        public bool IsArray { get; set; }
        [DataMember]
        public bool IsRequired { get; set; }
        [DataMember]
        public List<KeyValue> ExtensionProperties { get; set; }
    }
}
