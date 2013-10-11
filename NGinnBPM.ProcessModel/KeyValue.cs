using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{

    [DataContract]
    public class KeyValue
    {
        [DataMember]
        public string Key { get; set; }
        [DataMember]
        public string Value { get; set; }
    }
}
