using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogVisualizer.LogFileParsers
{
    public class LogParserFactory
    {
        public ILogFileParser CreateParserForLogFile(string path)
        {
            if (path.EndsWith(".xml"))
            {
                return new EventLogXmlParser();
            }
            else
            {
                return new TextLogFileParser();
            }
        }
    }
}
