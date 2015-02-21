using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            XmlSerializer xs = new XmlSerializer(typeof(tDefinitions));
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
