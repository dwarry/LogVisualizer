using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogVisualizer.Domain;

namespace LogVisualizer.LogFileParsers
{
    public class TextLogFileParser : ILogFileParser
    {
        private static readonly Regex _timestampRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:[.,]\d+))", 
            RegexOptions.Compiled);

        private static readonly Regex _logLevelRegex = new Regex(
            @"(?<=\s)(?:INFO|WARN(?:ING)?|DEBUG|ERROR|FATAL)(?=\s)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);


        public TimelineTree ParseDataAndBuildTree(Stream stream)
        {
            var result = new TimelineTree();

            var lineBreakLength = FindLineBreakLength();

            stream.Seek(0, SeekOrigin.Begin);

            using (var reader = new StreamReader(stream))
            {
                var offset = 0L;

                var line = reader.ReadLine();

                while (line != null)
                {
                    var match = _timestampRegex.Match(line);

                    if (match.Success)
                    {
                        var dt = NormalizeAndParseTimestamp(match);
                        result.Add(dt, offset);
                    }

                    offset += (reader.CurrentEncoding.GetByteCount(line) + lineBreakLength);
                    line = reader.ReadLine();
                }
            }

            return result;

            int FindLineBreakLength()
            {
                stream.Seek(0, SeekOrigin.Begin);

                var buffer = new byte[1024];

                var l = stream.Read(buffer, 0, buffer.Length);

                for(int i = 0; i < l; ++i)
                {
                    if(buffer[i] == 10)
                    {
                        if(i > 0 && buffer[i - 1] == 13)
                        {
                            return 2;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                }
                throw new InvalidDataException("Could not find a line break");
            }
        }

        private static DateTime NormalizeAndParseTimestamp(Match m)
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



        public IReadOnlyList<LogEntry> ReadEntries(Stream stream, long offset, int maxCount)
        {
            var results = new List<LogEntry>(maxCount);

            stream.Seek(offset, SeekOrigin.Begin);

            string id = "";
            LogLevel logLevel = LogLevel.Debug;

            var messageBuilder = new StringBuilder(2048);


            using(var reader = new StreamReader(stream))
            {
                var firstTimeThrough = true;
                while(results.Count < maxCount)
                {
                    var line = reader.ReadLine();

                    if(line == null)
                    {
                        break;
                    }

                    var m = _timestampRegex.Match(line);

                    if (m.Success)
                    {
                        if (firstTimeThrough)
                        {
                            firstTimeThrough = false;
                        }
                        else
                        {
                            var nextEntry = new LogEntry(id, logLevel, messageBuilder.ToString());

                            results.Add(nextEntry);
                        }

                        id = m.Value;

                        messageBuilder.Clear();
                        
                        messageBuilder.AppendLine(line);


                        var logLevelMatch = _logLevelRegex.Match(line);
                        
                        if (logLevelMatch.Success)
                        {
                            logLevel = ParseLogLevelMatch(logLevelMatch);
                        }
                        else
                        {
                            logLevel = LogLevel.None;
                        }
                    }
                    else
                    {
                        firstTimeThrough = false;

                        messageBuilder.AppendLine(line);
                    }
                }

            }

            if(messageBuilder.Length > 0)
            {
                results.Append(new LogEntry(id, logLevel, messageBuilder.ToString()));
            }

            return results.AsReadOnly();
        }

        private static LogLevel ParseLogLevelMatch(Match m)
        {
            switch (m.Value.ToLowerInvariant())
            {
                case "debug":
                    return LogLevel.Debug;
                case "info":
                    return LogLevel.Info;
                case "warn":
                case "warning":
                    return LogLevel.Warning;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                    return LogLevel.Fatal;
                default:
                    return LogLevel.None;
            }
        }
    }
}
