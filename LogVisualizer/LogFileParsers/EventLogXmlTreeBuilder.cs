using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LogVisualizer.Domain;

namespace LogVisualizer.LogFileParsers
{
    public class EventLogXmlTreeBuilder : ITimeLineTreeBuilder
    {
        public TimelineTree ParseDataAndBuildTree(Stream stream)
        {
            var result = new TimelineTree();

            var doc = XDocument.Load(stream, LoadOptions.SetLineInfo);

            XNamespace evtns = "http://schemas.microsoft.com/win/2004/08/events/event";

            var list = new List<(DateTime dt, long offs)>(4000);

            // /Events/Event[1]/System/TimeCreated/@SystemTime
            foreach (var evt in doc.Root.Elements())
            {
                var tc = evt.Descendants(evtns + "TimeCreated");
                var st = tc.Attributes("SystemTime").First().Value;
                var dt = DateTime.Parse(st, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                var lineInfo = (IXmlLineInfo)evt;
                var offset = (lineInfo.LineNumber * 1_000_000_000L) + lineInfo.LinePosition;

                list.Add((dt, offset));
            }

            foreach (var (dt, offs) in list.OrderBy(x => x.dt))
            {
                result.Add(dt, offs);
            }

            return result;
        }
    }
}
