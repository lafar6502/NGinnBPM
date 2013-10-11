using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    public enum DataBindingType
    {
        Literal,
        Expr,
        CopyVar
    }

    [DataContract]
    public class DataBindingDef
    {
        [DataMember]
        public string Target { get; set; }
        [DataMember]
        public string Source { get; set; }
        [DataMember]
        public DataBindingType BindType { get; set; }
    }
}
