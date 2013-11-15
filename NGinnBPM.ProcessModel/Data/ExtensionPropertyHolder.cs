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

        public static Dictionary<string, string> GetExtensionProperties(IDictionary<string, Dictionary<string, string>> props, string ns)
        {
            Dictionary<string, string> ret;
            return props != null && props.TryGetValue(ns, out ret) ? ret : new Dictionary<string, string>();
        }

        public static string GetExtensionProperty(IDictionary<string, Dictionary<string, string>> props, string ns, string name)
        {
            Dictionary<string, string> r;
            return props != null && props.TryGetValue(ns, out r) ? r.ContainsKey(name) ? r[name] : null : null;
        }

        public static void SetExtensionProperty(IDictionary<string, Dictionary<string, string>> props, string ns, string name, string value)
        {
            Dictionary<string, string> r;
            if (!props.TryGetValue(ns, out r))
            {
                r = new Dictionary<string, string>();
                props[ns] = r;
            }
            r.Remove(name);
            r[name] = value;
        }

        #endregion
    }
}
