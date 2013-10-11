using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using NLog;
using System.Reflection;
using System.IO;

namespace NGinnBPM.Lib.Schema.Xsd
{
    /// <summary>
    /// Resolver of xml references to files embedded in assembly
    /// </summary>
    public class AssemblyResourceXmlResolver : XmlUrlResolver
    {
        private static Logger log = LogManager.GetCurrentClassLogger();
        private string _defaultPrefix = "asm://NGinnBPM.Lib/Schema/Xsd/";
        private Assembly _defaultAsm = typeof(AssemblyResourceXmlResolver).Assembly;

        public AssemblyResourceXmlResolver(string defaultPrefix)
        {
        }

        public AssemblyResourceXmlResolver()
        {
        }

        public string DefaultPrefix
        {
            get { return _defaultPrefix; }
            set { _defaultPrefix = value; }
        }


        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            log.Info("GetEntity: {0}", absoluteUri.ToString());
            if (absoluteUri.Scheme == "asm")
            {
                string s = absoluteUri.LocalPath;
                if (s.StartsWith("/")) s = s.Substring(1);
                s = s.Replace('/', '.');
                Stream stm = _defaultAsm.GetManifestResourceStream(s);
                if (stm == null)
                    throw new Exception(string.Format("Resource '{0}' not found in assembly {1}", s, _defaultAsm.FullName));
                return stm;
            }
            else if (absoluteUri.Scheme == "assembly")
            {
                throw new NotImplementedException();
                /*IApplicationContext ctx = Spring.Context.Support.ContextRegistry.GetContext();
                IResource rc = ctx.GetResource(absoluteUri.OriginalString);
                if (rc == null)
                    throw new Exception("Schema resource not found: " + absoluteUri);
                return rc.InputStream;
                */
            }
            return base.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (baseUri == null || (!baseUri.IsAbsoluteUri && baseUri.OriginalString.Length == 0))
            {
                Uri u = new Uri(string.Format("{0}{1}", _defaultPrefix, relativeUri));
                return u;
            }
            log.Info("ResolveUri: {0} : {1}", baseUri.ToString(), relativeUri);
            return base.ResolveUri(baseUri, relativeUri);
        }
    }
}
