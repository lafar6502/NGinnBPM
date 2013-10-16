using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace NGinnBPM.ProcessModel.Util
{
    public class XmlConst
    {
        public static readonly string XmlSchemaNS = "http://www.w3.org/2001/XMLSchema";
        public static readonly XmlQualifiedName XS_string = new XmlQualifiedName("string", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_int = new XmlQualifiedName("int", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_date = new XmlQualifiedName("date", XmlSchemaNS);
        public static readonly XmlQualifiedName XS_dateTime = new XmlQualifiedName("dateTime", XmlSchemaNS);
        public static readonly string NGINN_PACKAGE_NAMESPACE = "http://www.nginn.org/PackageDefinition.1_0";
        public static readonly string NGINN_PROCESS_NAMESPACE = "http://www.nginn.org/WorkflowDefinition.1_0.xsd";
        public static readonly string NGINN_TASK_PERSISTENCE_NAMESPACE = "http://www.nginn.org/TaskInstance.xsd";


    }
}
