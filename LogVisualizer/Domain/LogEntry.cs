using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogVisualizer.Domain
{
    public struct LogEntry
    {
        public LogEntry(string id, LogLevel logLevel, string message)
        {
            Id = id;
            LogLevel = logLevel;
            Message = message ?? "";
        }

        public string Id { get; }

        public LogLevel LogLevel { get; }

        public string Message { get; }
    }
}
