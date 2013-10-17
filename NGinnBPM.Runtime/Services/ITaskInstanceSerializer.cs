using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using NGinnBPM.Runtime.Tasks;

namespace NGinnBPM.Runtime.Services
{
    public interface ITaskInstanceSerializer
    {
        string Serialize(TaskInstance ti, out string taskTypeId);
        TaskInstance Deserialize(string data, string taskTypeId);
    }

    public class JsonTaskInstanceSerializer : ITaskInstanceSerializer
    {
        private JsonSerializer _ser;

        public JsonTaskInstanceSerializer()
        {
            JsonSerializerSettings s = new JsonSerializerSettings();
            s.TypeNameHandling = TypeNameHandling.Objects;
            s.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            s.NullValueHandling = NullValueHandling.Ignore;
            s.MissingMemberHandling = MissingMemberHandling.Ignore;
            s.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            _ser = JsonSerializer.Create(s);
        }

        public string Serialize(TaskInstance ti, out string taskTypeId)
        {
            StringWriter sw = new StringWriter();
            _ser.Serialize(new JsonTextWriter(sw), ti);
            taskTypeId = ti.GetType().FullName;
            return sw.ToString();
        }

        public TaskInstance Deserialize(string data, string taskTypeId)
        {
            return (TaskInstance) _ser.Deserialize(new JsonTextReader(new StringReader(data)));
        }
    }
}
