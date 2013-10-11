using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NGinnBPM.ProcessModel;
using System.Runtime.Serialization;
using System.Collections;

namespace NGinnBPM.ProcessModel.Data
{
    /// <summary>
    /// Variable definition - used for defining process data schemas
    /// </summary>
    [Serializable]
    [DataContract]
    public class VariableDef : MemberDef
    {
        public enum Dir
        {
            Local,
            In,
            Out,
            InOut,
        }
        
        public VariableDef()
        {
        }

        public VariableDef(VariableDef rhs) : base(rhs)
        {
            VariableDir = rhs.VariableDir; DefaultValueExpr = rhs.DefaultValueExpr;
        }

        public VariableDef(string name, string typeName, bool isArray, bool isRequired, Dir variableDir, string defaultExpr) : base(name, typeName, isArray, isRequired)
        {
            VariableDir = variableDir; DefaultValueExpr = defaultExpr;
        }

        [DataMember]
        public Dir VariableDir { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string DefaultValueExpr { get; set; }
        

        public override void LoadFromXml(XmlElement el, XmlNamespaceManager nsmgr)
        {
            base.LoadFromXml(el, nsmgr);
            string pr = nsmgr.LookupPrefix(XmlSchemaUtil.WORKFLOW_NAMESPACE);
            if (pr != null && pr.Length > 0) pr += ":";
            VariableDir = (VariableDef.Dir)Enum.Parse(typeof(VariableDef.Dir), el.GetAttribute("dir"));
            DefaultValueExpr = XmlSchemaUtil.GetXmlElementText(el, pr + "defaultValue", nsmgr);
        }

        
    }

}
