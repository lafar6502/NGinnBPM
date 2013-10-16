using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NGinnBPM.ProcessModel.Util
{
    public class TaskDataJsonConverter : JsonConverter
    {
        private JsonSerializer _mySer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        private object ConvertJToken(JToken tok)
        {
            if (tok is JObject)
            {
                JObject job = (JObject)tok;
                Dictionary<string, object> dob = new Dictionary<string, object>();
                foreach (JProperty prop in job.Properties())
                {
                    dob[prop.Name] = ConvertJToken(prop.Value);
                }
                return dob;
            }
            else if (tok is JArray)
            {
                JArray jar = (JArray)tok;
                List<object> lst = new List<object>();
                JToken jt = jar.First;
                while (jt != null)
                {
                    lst.Add(ConvertJToken(jt));
                    jt = jt.Next;
                }
                return lst;
            }
            else if (tok is JValue)
            {
                return ((JValue)tok).Value;
            }
            else
            {
                throw new Exception("Unhandled token type: " + tok.GetType());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject job = JObject.Load(reader);
            return ConvertJToken(job);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (serializer.TypeNameHandling == TypeNameHandling.None)
            {
                serializer.Serialize(writer, value);
            }
            else
            {
                _mySer.Serialize(writer, value);
            }
        }
    }
}
