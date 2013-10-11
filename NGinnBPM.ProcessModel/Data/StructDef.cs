using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;

namespace NGinnBPM.ProcessModel.Data
{
    [Serializable]
    [DataContract]
    public class StructDef : TypeDef
    {
        public StructDef()
        {
            Members = new List<MemberDef>();
        }

        [DataMember]
        public List<MemberDef> Members { get; set; }

        [IgnoreDataMember]
        public override bool IsSimpleType
        {
            get { return false; }
        }

        public MemberDef GetMember(string name)
        {
            return Members.Find(x => x.Name == name);
        }

        public override void WriteXmlSchemaType(XmlWriter xw, string xmlns)
        {
            xw.WriteStartElement("complexType", XmlSchemaUtil.SCHEMA_NS);
            if (Name != null) xw.WriteAttributeString("name", Name);
            xw.WriteStartElement("sequence", XmlSchemaUtil.SCHEMA_NS);
            foreach (MemberDef member in Members)
            {
                xw.WriteStartElement("element", XmlSchemaUtil.SCHEMA_NS);
                xw.WriteAttributeString("name", member.Name);
                TypeDef td = ParentTypeSet.GetTypeDef(member.TypeName);
                if (td.IsSimpleType)
                {
                    xw.WriteAttributeString("type", "xs:" + td.Name);
                    if (member.IsRequired)
                        xw.WriteAttributeString("nillable", "false");
                }
                else
                {
                    if (string.IsNullOrEmpty(xmlns))
                        xw.WriteAttributeString("type", td.Name);
                    else
                    {
                        string prf = xw.LookupPrefix(xmlns);
                        xw.WriteAttributeString("type", string.Format("{0}:{1}", prf, td.Name));
                    }
                }
                xw.WriteAttributeString("minOccurs", member.IsRequired ? "1" : "0");
                xw.WriteAttributeString("maxOccurs", member.IsArray ? "unbounded" : "1");
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
            foreach (XmlElement mel in el.SelectNodes(pr + "member", nsmgr))
            {
                MemberDef md = new MemberDef();
                md.LoadFromXml(mel, nsmgr);
                Members.Add(md);
            }
        }

        public override TypeDef CloneTypeDef()
        {
            StructDef sd = new StructDef();
            sd.Name = this.Name;
            foreach (MemberDef md in Members)
            {
                sd.Members.Add(md.CloneMemberDef());
            }
            return sd;
        }

        public override object ConvertToValidType(object input)
        {
            if (input == null) return null;
            if (input is Dictionary<string, object>) return input;
            if (input is System.Collections.IDictionary) return input;
            if (input is IDictionary<string, object>) return input;
            throw new Exceptions.DataValidationException(string.Format("Invalid value for struct type {0}: {1}", Name, input));
        }

    }
}
