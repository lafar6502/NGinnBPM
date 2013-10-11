using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel.Data
{
    [Serializable]
    public class ExtensionPropertyHelper
    {
        public static bool SplitFullName(string fullName, out string ns, out string name)
        {
            ns = null;
            name = fullName;
            int idx = fullName.IndexOf(':');
            if (idx < 0) return false;
            ns = fullName.Substring(0, idx);
            name = fullName.Substring(idx + 1);
            return true;
        }

        #region IHaveExtensionProperties Members

        public static IEnumerable<string> GetExtensionProperties(IDictionary<string, string> props, string ns)
        {
            List<string> lst = new List<string>();
            if (props == null) return lst;
            string s = ns + ":";
            foreach (string k in props.Keys)
            {
                if (k.StartsWith(s))
                    lst.Add(k);
            }
            return lst;
        }

        public static string GetExtensionProperty(IDictionary<string, string> props, string ns, string name)
        {
            string s = string.Format("{0}:{1}", ns, name);
            return GetExtensionProperty(props, s);
        }

        public static string GetExtensionProperty(IDictionary<string, string> props, string fullName)
        {
            if (props == null) return null;
            return props.ContainsKey(fullName) ? props[fullName] : null;
        }

        public static void SetExtensionProperty(IDictionary<string, string> props, string ns, string name, string value)
        {
            string s = string.Format("{0}:{1}", ns, name);
            lock (props)
            {
                if (props.ContainsKey(s))
                    props.Remove(s);
                props[s] = value;
            }
        }

        #endregion
    }
}
