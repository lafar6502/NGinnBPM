using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NGinnBPM.ProcessModel
{
    /// <summary>
    /// Interface for accessing custom properties of process definition components.
    /// Custom properties can be used by external tools to specify additional information.
    /// </summary>
    public interface IHaveMetadata
    {
        Dictionary<string, object> GetMetadata(string ns);
        object GetMetaValue(string ns, string name);
        void SetMetaValue(string ns, string name, object value);
    }
}
