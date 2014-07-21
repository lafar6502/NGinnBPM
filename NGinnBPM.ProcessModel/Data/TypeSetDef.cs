using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinnBPM.ProcessModel;
using System.Xml.XPath;
using System.Runtime.Serialization;

namespace NGinnBPM.ProcessModel.Data
{
    
    [DataContract]
    public class TypeSet
    {
        public static readonly SimpleTypeDef TYPE_STRING = new SimpleTypeDef("string", typeof(string));
        public static readonly SimpleTypeDef TYPE_INT = new SimpleTypeDef("int", typeof(Int32));
        public static readonly SimpleTypeDef TYPE_DOUBLE = new SimpleTypeDef("double", typeof(double));
        public static readonly SimpleTypeDef TYPE_DATE = new SimpleTypeDef("date", typeof(DateTime));
        public static readonly SimpleTypeDef TYPE_DATETIME = new SimpleTypeDef("dateTime", typeof(DateTime));
        public static readonly SimpleTypeDef TYPE_BOOL = new SimpleTypeDef("bool", typeof(bool));
        public static readonly SimpleTypeDef TYPE_DOCREF = new SimpleTypeDef("docref", typeof(string));
        
        private static Dictionary<string, TypeDef> _builtInTypes;
        
        static TypeSet()
        {
            _builtInTypes = new Dictionary<string, TypeDef>();
            _builtInTypes[TYPE_STRING.Name] = TYPE_STRING;
            _builtInTypes[TYPE_INT.Name] = TYPE_INT;
            _builtInTypes[TYPE_DOUBLE.Name] = TYPE_DOUBLE;
            _builtInTypes[TYPE_DATE.Name] = TYPE_DATE;
            _builtInTypes[TYPE_DATETIME.Name] = TYPE_DATETIME;
            _builtInTypes[TYPE_BOOL.Name] = TYPE_BOOL;
        }

        public TypeSet()
        {
            Types = new Dictionary<string, TypeDef>();
        }

        [DataMember]
        public Dictionary<string, TypeDef> Types { get; set; }

        public bool IsBasicType(string typeName)
        {
            TypeDef td = GetTypeDef(typeName);
            if (td is SimpleTypeDef) return true;
            return false;
        }

        public bool IsEnumType(string typeName)
        {
            TypeDef td = GetTypeDef(typeName);
            if (td is EnumDef) return true;
            return false;
        }

        public bool IsTypeDefined(string typeName)
        {
            return GetTypeDef(typeName) != null;
        }

        public TypeDef GetTypeDef(string name)
        {
            TypeDef td;
            if (_builtInTypes.TryGetValue(name, out td))
                return td;
            if (Types.TryGetValue(name, out td))
                return td;
            if (name == TYPE_BOOL.Name)
                return TYPE_BOOL;
            else if (name == TYPE_DATETIME.Name)
                return TYPE_DATETIME;
            else if (name == TYPE_DATE.Name)
                return TYPE_DATE;
            else if (name == TYPE_DOUBLE.Name)
                return TYPE_DOUBLE;
            else if (name == TYPE_INT.Name)
                return TYPE_INT;
            else if (name == TYPE_STRING.Name)
                return TYPE_STRING;
            return null;
        }

        public StructDef GetStructType(string name)
        {
            return GetTypeDef(name) as StructDef;
        }

        public void AddType(TypeDef sd)
        {
            List<TypeDef> l = new List<TypeDef>();
            l.Add(sd);
            AddTypes(l);
        }

        public void AddTypes(ICollection<TypeDef> types)
        {
            ValidationCtx ctx = new ValidationCtx();
            foreach (TypeDef sd in types)
            {
                if (IsTypeDefined(sd.Name)) throw new ApplicationException("Type already defined: " + sd.Name);
                ctx.NewTypes.Add(sd.Name, sd);
            }
            foreach (TypeDef sd in types)
            {
                ValidateTypeDef(sd, ctx);
            }
            foreach (TypeDef sd in types)
            {
                sd.ParentTypeSet = this;
                Types.Add(sd.Name, sd);
            }
        }

        
        private class ValidationCtx
        {
            public Dictionary<string, TypeDef> NewTypes = new Dictionary<string, TypeDef>();
            public Dictionary<string, TypeDef> ValidatedTypes = new Dictionary<string, TypeDef>();
        }

        private void ValidateTypeDef(TypeDef td, ValidationCtx ctx)
        {
            if (IsTypeDefined(td.Name)) return;
            if (ctx.ValidatedTypes.ContainsKey(td.Name)) return;
            ctx.ValidatedTypes.Add(td.Name, td);
            if (td is SimpleTypeDef)
            {
                return;
            }
            else if (td is StructDef)
            {
                StructDef sd = (StructDef)td;
                foreach (MemberDef md in sd.Members)
                {
                    if (!IsTypeDefined(md.TypeName))
                    {
                        if (!ctx.NewTypes.ContainsKey(md.TypeName))
                        {
                            throw new ApplicationException(string.Format("Member type ({0}) not defined for {1}.{2}", md.TypeName, sd.Name, md.Name));
                        }
                        TypeDef td2 = ctx.NewTypes[md.TypeName];
                        ValidateTypeDef(td2, ctx);
                    }
                }
            }
            else if (td is EnumDef)
            {
                EnumDef ed = (EnumDef)td;
            }
            else throw new Exception();
        }

        [IgnoreDataMember]
        public ICollection<string> TypeNames
        {
            get
            {
                return Types.Keys;
            }
        }

        /// <summary>
        /// Generate XML schema for the whole type set
        /// Warning: does not write xs schema declaration but the contents only
        /// </summary>
        /// <param name="xw"></param>
        public void WriteXmlSchema(XmlWriter xw)
        {
            WriteXmlSchema(xw, null);
        }

        public void WriteXmlSchema(XmlWriter xw, string xmlns)
        {
            foreach (string tdName in TypeNames)
            {
                TypeDef td = GetTypeDef(tdName);
                td.WriteXmlSchemaType(xw, xmlns);
            }
        }





        

        /// <summary>
        /// Return complete xml data schema for given structDef as a root element
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="rootElementName"></param>
        /// <param name="inputNamespace"></param>
        /// <returns></returns>
        public string GetDataSchema(StructDef rootElement, string rootElementName, string inputNamespace)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriterSettings ws = new XmlWriterSettings();
            ws.OmitXmlDeclaration = true; ws.Indent = true;
            XmlWriter xw = XmlWriter.Create(sb, ws);
            xw.WriteStartDocument();
            WriteXmlSchemaForStruct(rootElement, rootElementName, inputNamespace, xw);
            xw.WriteEndDocument();
            xw.Flush();
            return sb.ToString();
        }

        /// <summary>
        /// Write complete XML schema 
        /// </summary>
        /// <param name="rootElement"></param>
        /// <param name="rootElementName"></param>
        /// <param name="inputDataNamespace"></param>
        /// <param name="xw"></param>
        public void WriteXmlSchemaForStruct(StructDef rootElement, string rootElementName, string inputDataNamespace, XmlWriter xw)
        {
            xw.WriteStartElement("xs", "schema", XmlSchemaUtil.SCHEMA_NS);
            if (inputDataNamespace != null && inputDataNamespace.Length > 0)
            {
                xw.WriteAttributeString("xmlns", inputDataNamespace);
            }

            WriteXmlSchema(xw);
            if (rootElement != null)
            {
                xw.WriteStartElement("element", XmlSchemaUtil.SCHEMA_NS);
                xw.WriteAttributeString("name", rootElementName);
                rootElement.WriteXmlSchemaType(xw);
                xw.WriteEndElement();
                xw.WriteEndElement();
            }
        }

        /// <summary>
        /// Import types from given typeset.
        /// Does not import already existing types.
        /// </summary>
        /// <param name="sourceTypeSet"></param>
        public void ImportTypes(TypeSet sourceTypeSet)
        {
            foreach (string tn in sourceTypeSet.TypeNames)
            {
                TypeDef td = sourceTypeSet.GetTypeDef(tn);
                if (!IsTypeDefined(td.Name))
                    ImportType(td);
            }
        }

        private void ImportType(TypeDef td)
        {
            if (this.IsTypeDefined(td.Name))
            {
                return;
            };
            if (td is StructDef)
            {
                ImportStruct((StructDef)td);
            }
            else
            {
                AddType(td.CloneTypeDef());
            }
        }

        private void ImportStruct(StructDef sd)
        {
            foreach (MemberDef md in sd.Members)
            {
                if (!IsTypeDefined(md.TypeName))
                {
                    TypeDef td = sd.ParentTypeSet.GetTypeDef(md.TypeName);
                    if (td == null) throw new Exception("Undefined type: " + td.Name);
                    ImportType(td);
                }
            }
            StructDef newStruct = (StructDef) sd.CloneTypeDef();
            AddType(newStruct);
        }

        public void LoadXml(XmlElement rootNode, XmlNamespaceManager nsmgr)
        {
            string pr = nsmgr.LookupPrefix(XmlSchemaUtil.WORKFLOW_NAMESPACE);
            pr = (pr != null && pr.Length > 0) ? pr + ":" : "";
            foreach (XmlElement el in rootNode.ChildNodes)
            {
                if (el.LocalName == "struct")
                {
                    StructDef sd = new StructDef();
                    sd.LoadFromXml(el, nsmgr);
                    AddType(sd);
                }
                else if (el.LocalName == "enum")
                {
                    EnumDef ed = new EnumDef();
                    ed.LoadFromXml(el, nsmgr);
                    AddType(ed);
                }
                else throw new Exception("Unexpected node: " + el.Name);
            }
        }

    }
}
