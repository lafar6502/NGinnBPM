using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinnBPM.ProcessModel;
using System.Runtime.Serialization;
using System.Collections;

namespace NGinnBPM.ProcessModel.Data
{
    [Serializable]
    [DataContract]
    [KnownType(typeof(VariableDef))]
    public class MemberDef : IHaveExtensionProperties
    {
        public MemberDef()
        {
        }

        public MemberDef(MemberDef rhs) : this(rhs.Name, rhs.TypeName, rhs.IsArray, rhs.IsRequired)
        {
        }

        public MemberDef(string name, string typeName, bool isArray, bool isRequired)
        {
            Name = name; TypeName = typeName; IsArray = isArray; IsRequired = isRequired;
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public bool IsArray { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool IsRequired { get; set; }

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public Dictionary<string, string> ExtensionProperties { get; set; }

        public virtual void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            Name = el.GetAttribute("name");
            TypeName = el.GetAttribute("type");
            string t = el.GetAttribute("isArray");
            IsArray = "true".Equals(t) || "1".Equals(t);
            t = el.GetAttribute("required");
            IsRequired = t == null || t.Length == 0 || "true".Equals(t) || "1".Equals(t);
        }

        public virtual MemberDef CloneMemberDef()
        {
            MemberDef md = new MemberDef(this);
            return md;
        }

        public object ConvertToValidMemberValue(TypeSet types, object v)
        {
            TypeDef td = types.GetTypeDef(this.TypeName);
            if (this.IsArray)
            {
                List<object> ret = new List<object>();
                IEnumerable enu = v as IEnumerable;
                if (enu != null)
                {
                    foreach (object v2 in enu)
                        ret.Add(td.ConvertToValidType(v2));
                }
                else
                {
                    ret.Add(td.ConvertToValidType(v));
                }
                return ret;
            }
            else
                return td.ConvertToValidType(v);
        }



        #region IHaveExtensionProperties Members

        public IEnumerable<string> GetExtensionProperties(string xmlns)
        {
            return ExtensionPropertyHelper.GetExtensionProperties(ExtensionProperties, xmlns);
        }

        public string GetExtensionProperty(string xmlns, string name)
        {
            return ExtensionPropertyHelper.GetExtensionProperty(ExtensionProperties, xmlns, name);
        }

        public void SetExtensionProperty(string xmlns, string name, string value)
        {
            if (ExtensionProperties == null) ExtensionProperties = new Dictionary<string, string>();
            ExtensionPropertyHelper.SetExtensionProperty(ExtensionProperties, xmlns, name, value);
        }

        public string GetExtensionProperty(string fullName)
        {
            return ExtensionPropertyHelper.GetExtensionProperty(ExtensionProperties, fullName);
        }

        #endregion
    }
}
