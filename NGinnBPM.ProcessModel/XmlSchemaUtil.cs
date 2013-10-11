using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using System.Xml;
using System.Reflection;

namespace NGinnBPM.ProcessModel
{
    public class XmlSchemaUtil
    {
        public static readonly string WORKFLOW_NAMESPACE = "";
        public static readonly string SCHEMA_NS = "http://www.w3.org/2001/XMLSchema";
        public static readonly string WSDL_NS = "http://schemas.xmlsoap.org/wsdl/";
        public static readonly string SOAP_NS = "http://schemas.xmlsoap.org/wsdl/soap/";
        public static readonly string XSL_NS = "http://www.w3.org/1999/XSL/Transform";
        public static readonly string SOAP12_NS = "http://schemas.xmlsoap.org/wsdl/soap12/";
        public static readonly string SOAP_ENV = "http://schemas.xmlsoap.org/soap/envelope/";
        
        public static string GetXmlElementText(XmlElement parent, string xpath, XmlNamespaceManager nsmgr)
        {
            XmlNode t = parent.SelectSingleNode(xpath, nsmgr);
            if (t == null) return null;
            if (t is XmlElement)
                return t.InnerText;
            else
                return t.Value;
        }
    }

    
}
