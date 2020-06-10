using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LogVisualizer.Domain;

namespace LogVisualizer.LogFileParsers
{
    /// <summary>
    /// Represents a Strategy for parsing a log file and building a
    /// TimeLineTree of the events contained within. 
    /// </summary>
    public  interface ILogFileParser
    {
        /// <summary>
        /// Read the data from the stream and prepare the Tree containing the 
        /// counts and offsets at the different levels of time granularity
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        TimelineTree ParseDataAndBuildTree(Stream stream);

        /// <summary>
        /// Read data from the stream and generates a list of up to a maximum number
        /// of <c>LogEntry</c>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="offset"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        IReadOnlyList<LogEntry> ReadEntries(Stream stream, long offset, int maxCount);
    }
}