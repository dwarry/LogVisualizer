using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogVisualizer.Domain;

namespace LogVisualizer.LogFileParsers
{
    public class TextLogFileTreeBuilder : ITimeLineTreeBuilder
    {
        private static readonly Regex _timestampRegex = new Regex(@"^(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:[.,]\d+))");

        public TimelineTree ParseDataAndBuildTree(Stream stream)
        {
            var result = new TimelineTree();

            using (var reader = new StreamReader(stream))
            {
                var offset = reader.BaseStream.Position;

                var line = reader.ReadLine();

                while (line != null)
                {
                    var match = _timestampRegex.Match(line);

                    if (match.Success)
                    {
                        var dt = normalizeAndParseTimestamp(match);
                        result.Add(dt, offset);
                    }

                    offset = reader.BaseStream.Position;
                    line = reader.ReadLine();
                }
            }

            return result;


            DateTime normalizeAndParseTimestamp(Match m)
            {
                var sb = new StringBuilder(m.Value);

                sb[10] = 'T';

                if (sb.Length == 19)
                {
                    sb.Append('.');
                }
                else
                {
                    sb[19] = '.';
                }

                while (sb.Length < 23)
                {
                    sb.Append('0');
                }

                sb.Append('Z');// assume UTC

                return DateTime.ParseExact(sb.ToString(),
                    "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'",
                    DateTimeFormatInfo.InvariantInfo);
            }
        }
    }
}
