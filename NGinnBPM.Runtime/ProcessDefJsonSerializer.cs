using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NGinnBPM.ProcessModel;
using System.IO;
using Newtonsoft.Json.Linq;

namespace NGinnBPM.Runtime
{
    /// <summary>
    /// Handles process definition json serialization and deserialization
    /// </summary>
    public class ProcessDefJsonSerializer
    {
        private static JsonSerializer _ser;

        protected class TaskTypeConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(TaskDef);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JObject ob = JObject.Load(reader);
                string s = ob.Value<string>("TaskType");
                if (string.IsNullOrEmpty(s)) s = ob.Value<string>("$type");
                if (s == null) throw new Exception("task type missing: " + ob.ToString());
                bool composite = s.IndexOf("composite", StringComparison.InvariantCultureIgnoreCase) >= 0;
                TaskDef tsk = composite ? (TaskDef) new CompositeTaskDef() : new AtomicTaskDef();
                serializer.Populate(ob.CreateReader(), tsk);
                return tsk;                
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value);
            }


            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return false;
                }
            }
        }

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
            sett.Converters.Add(new TaskTypeConverter());

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
