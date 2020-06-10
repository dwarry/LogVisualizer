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
    public class EventLogXmlParser : ILogFileParser
    {
        private const long LINE_MULTIPLIER = 1_000_000_000;

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
                var offset = (lineInfo.LineNumber * LINE_MULTIPLIER) + lineInfo.LinePosition;

                list.Add((dt, offset));
            }

            foreach (var (dt, offs) in list.OrderBy(x => x.dt))
            {
                result.Add(dt, offs);
            }

            return result;
        }

        public IReadOnlyList<LogEntry> ReadEntries(Stream stream, long offset, int maxCount)
        {
            var line = Math.DivRem(offset, LINE_MULTIPLIER, out var position);

            // TODO: read through the file until you get ot he element

            return new List<LogEntry>
            {
                new LogEntry("", LogLevel.Error, "Not supported yet")
            }.AsReadOnly();
        }
    }
}
