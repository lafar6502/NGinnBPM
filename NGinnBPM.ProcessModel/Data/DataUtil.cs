using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace NGinnBPM.ProcessModel.Data
{
    public class DataUtil
    {
        public static void Validate(IDictionary<string, object> data, StructDef recType)
        {
        }

        public static void ToXml(IDictionary<string, object> data, TextWriter output)
        {
        }

        public static Dictionary<string, object> ReadXml(TextReader input)
        {
            throw new NotImplementedException();
        }

        public static Dictionary<string, object> ReadJson(TextReader tr)
        {
            throw new NotImplementedException();
        }

        public static string ToXml(Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

        public static string ToJson(Dictionary<string, object> data)
        {
            throw new NotImplementedException();
        }

    }
}
