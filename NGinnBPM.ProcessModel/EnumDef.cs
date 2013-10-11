using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Enum")]
    public class EnumDef :  TypeDef
    {
        [DataMember]
        public List<string> EnumValues { get; set; }
    }
}
