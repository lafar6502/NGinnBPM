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

        public static Dictionary<string, object> GetExtensionProperties(IDictionary<string, Dictionary<string, object>> props, string ns)
        {
            Dictionary<string, object> ret;
            return props != null && props.TryGetValue(ns, out ret) ? ret : new Dictionary<string, object>();
        }

        public static object GetExtensionProperty(IDictionary<string, Dictionary<string, object>> props, string ns, string name)
        {
            Dictionary<string, object> r;
            return props != null && props.TryGetValue(ns, out r) ? r.ContainsKey(name) ? r[name] : null : null;
        }

        public static void SetExtensionProperty(IDictionary<string, Dictionary<string, object>> props, string ns, string name, object value)
        {
            Dictionary<string, object> r;
            if (!props.TryGetValue(ns, out r))
            {
                r = new Dictionary<string, object>();
                props[ns] = r;
            }
            r.Remove(name);
            r[name] = value;
        }

        #endregion
    }
}
