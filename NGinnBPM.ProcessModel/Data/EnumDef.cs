using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinnBPM.ProcessModel;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel.Data
{
    /// <summary>
    /// Enumeration type definition
    /// </summary>
    [Serializable]
    [DataContract]
    public class EnumDef : TypeDef
    {
        public EnumDef()
        {
            EnumValues = new List<string>();
        }

        [DataMember]
        public List<string> EnumValues { get; set; }

        [IgnoreDataMember]
        public override bool IsSimpleType
        {
            get { return false; }
        }

        public void AddEnumValue(string val)
        {
            EnumValues.Add(val);
        }

        

        public override void WriteXmlSchemaType(System.Xml.XmlWriter xw, string xmlns)
        {
            xw.WriteStartElement("simpleType", XmlSchemaUtil.SCHEMA_NS);
            if (Name != null) xw.WriteAttributeString("name", Name);
            xw.WriteStartElement("restriction", XmlSchemaUtil.SCHEMA_NS);
            xw.WriteAttributeString("base", "xs:string");
            foreach(object val in this.EnumValues)
            {
                xw.WriteStartElement("enumeration", XmlSchemaUtil.SCHEMA_NS);
                xw.WriteAttributeString("value", val.ToString());
                xw.WriteEndElement();
            }
            xw.WriteEndElement();
            xw.WriteEndElement();
        }

        public void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(XmlSchemaUtil.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            Name = el.GetAttribute("name");
            foreach(XmlElement v in el.SelectNodes(pr + "value", nsmgr))
            {
                string sv = v.InnerText;
                
                //object ev = Convert.ChangeType(sv, BaseType.ValueType);
                EnumValues.Add(sv);
            }
        }

        public override TypeDef CloneTypeDef()
        {
            EnumDef ed = new EnumDef();
            ed.Name = this.Name;
            ed.EnumValues = new List<string>(this.EnumValues);
            return ed;
        }

        public override object ConvertToValidType(object input)
        {
            if (input == null) return null;
            string val = input.ToString();
            foreach (object v in this.EnumValues)
            {
                if (v.Equals(val)) return val;
            }
            throw new Exceptions.DataValidationException(string.Format("Enum {0} does not contain value {1}", this.Name, input));
        }
    }
}
