using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace NGinnBPM.ProcessModel.Util
{
    /// <summary>
    /// Omits $type - this is a workaround for missing TypeNameHandling.Auto in .net 3.5
    /// </summary>
    public class NoTypeJsonConverter : JsonConverter
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

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, objectType);
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
