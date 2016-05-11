using System;
using System.IO;
using System.Xml;

using Org.Omg.BPMN20;
using System.Xml.Serialization;

namespace NGinnBPM.BPMNTools.Parser
{
    public class BPMNParser
    {
        public static tDefinitions Parse(TextReader tr)
        {
            var xs = new XmlSerializer(typeof(tDefinitions));
            var td = xs.Deserialize(tr);

            var sw = new StringWriter();
            var xtw = XmlWriter.Create(sw, new XmlWriterSettings {
                Indent = true
            });
            xs.Serialize(xtw, td);
            xtw.Flush();
            Console.WriteLine(sw.ToString());

            return (tDefinitions)td;
        }
    }
}
