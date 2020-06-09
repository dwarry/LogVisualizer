using System;
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
    public  interface ITimeLineTreeBuilder
    {
        TimelineTree ParseDataAndBuildTree(Stream stream);
    }
}