using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogVisualizer.LogFileParsers
{
    public class TreeBuilderFactory
    {
        public ITimeLineTreeBuilder CreateTreeBuilderForLogFile(string path)
        {
            if (path.EndsWith(".xml"))
            {
                return new EventLogXmlTreeBuilder();
            }
            else
            {
                return new TextLogFileTreeBuilder();
            }
        }
    }
}
