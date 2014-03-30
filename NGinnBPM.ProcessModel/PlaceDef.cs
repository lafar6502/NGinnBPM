using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel
{
    [DataContract(Name="Place")]
    public class PlaceDef : NodeDef
    {
        public PlaceDef()
        {
            this.PlaceType = PlaceTypes.Intermediate;
        }

        [DataMember]
        public PlaceTypes PlaceType { get; set; }
        [DataMember]
        public bool Implicit { get; set; }

        public override bool Validate(List<string> problemsFound)
        {
            return true;
        }
    }
}
