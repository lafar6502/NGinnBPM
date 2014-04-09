using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NGinnBPM.ProcessModel;
using System.IO;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// Handles process definition json serialization and deserialization
    /// </summary>
    public class ProcessDefJsonSerializer
    {
        private static JsonSerializer _ser;

        static ProcessDefJsonSerializer()
        {
            var sett = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            sett.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            _ser = JsonSerializer.Create(sett);
        }

        public static void Serialize(ProcessDef pd, TextWriter output)
        {
            _ser.Serialize(new JsonTextWriter(output), pd);
        }

        public static string Serialize(ProcessDef pd)
        {
            var sw = new StringWriter();
            Serialize(pd, sw);
            return sw.ToString();
        }

        public static ProcessDef Deserialize(TextReader tr)
        {
            return _ser.Deserialize<ProcessDef>(new JsonTextReader(tr));
        }

        public static ProcessDef Deserialize(string json)
        {
            return Deserialize(new StringReader(json));
        }

        public static ProcessDef DeserializeFile(string fileName)
        {
            using (var sr = new StreamReader(fileName, Encoding.UTF8))
            {
                return Deserialize(sr);
            }
        }

        public static void SerializeToFile(ProcessDef pd, string fileName)
        {
            using (var sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                Serialize(pd, sw);
            }
        }
    }
}
