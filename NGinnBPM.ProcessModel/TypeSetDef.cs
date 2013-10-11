using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{

    [DataContract(Name="Typeset")]
    public class TypeSetDef
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        List<TypeDef> Types { get; set; }
    }
}
