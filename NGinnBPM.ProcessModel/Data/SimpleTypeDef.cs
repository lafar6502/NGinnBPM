using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel.Data
{


    [Serializable]
    public class SimpleTypeDef : TypeDef
    {
        private Type _valueType;

        public Type ValueType
        {
            get { return _valueType; }
            set { _valueType = value; }
        }



        public SimpleTypeDef()
        {
        }

        public SimpleTypeDef(string name, Type valueType)
        {
            Name = name;
            ValueType = valueType;
        }

        public override bool IsSimpleType
        {
            get { return true; }
        }

        public override TypeDef CloneTypeDef()
        {
            SimpleTypeDef sd = new SimpleTypeDef();
            sd.Name = Name;
            sd.ValueType = ValueType;
            return sd;
        }

        public override object ConvertToValidType(object input)
        {
            if (input == null) return null;
            if (this.ValueType == typeof(bool))
            {
                if ("true".Equals(input) || "1".Equals(input)) return true;
            }
            return Convert.ChangeType(input, this.ValueType);
        }

        public override void WriteXmlSchemaType(System.Xml.XmlWriter xw, string xmlns)
        {
        }
    }

}
