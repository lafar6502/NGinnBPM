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
    [KnownType(typeof(SimpleTypeDef))]
    [KnownType(typeof(EnumDef))]
    [KnownType(typeof(StructDef))]
    public abstract class TypeDef : IHaveExtensionProperties
    {

        [IgnoreDataMember]
        private TypeSet _typeSet;

        [DataMember]
        public string Name { get; set; }

        [IgnoreDataMember]
        public abstract bool IsSimpleType
        {
            get;
        }

        [IgnoreDataMember]
        public TypeSet ParentTypeSet
        {
            get { return _typeSet; }
            set { _typeSet = value; }
        }

        public virtual void WriteXmlSchemaType(XmlWriter xw)
        {
            WriteXmlSchemaType(xw, null);
        }

        public abstract void WriteXmlSchemaType(XmlWriter xw, string xmlns);
        

        public abstract TypeDef CloneTypeDef();

        public abstract object ConvertToValidType(object input);

        #region IHaveExtensionProperties Members

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Dictionary<string, string> ExtensionProperties { get; set; }

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
