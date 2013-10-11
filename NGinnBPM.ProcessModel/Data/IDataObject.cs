using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;

namespace NGinnBPM.ProcessModel.Data
{
    /// <summary>
    /// Basic interface for dynamic data container
    /// </summary>
    public interface IDynData
    {
        ICollection<string> FieldNames { get; }
        object this[string idx] { get; set; }
    }




}
