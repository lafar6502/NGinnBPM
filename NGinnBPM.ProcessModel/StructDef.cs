using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Struct")]
    public class StructDef : TypeDef
    {
        public List<MemberDef> Members { get; set; }
    }
}
