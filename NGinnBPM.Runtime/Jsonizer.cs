using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace NGinnBPM.Runtime
{
    public class Jsonizer
    {
        private static JsonSerializer ser;
        static Jsonizer()
        {
            JsonSerializerSettings s= new JsonSerializerSettings {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() },
                Formatting = Formatting.Indented
            };
            ser = JsonSerializer.Create(s);
        }

        public static string ToJsonString(object v)
        {
            var sw = new StringWriter();
            ser.Serialize(sw, v);
            return sw.ToString();
        }
    }
}
