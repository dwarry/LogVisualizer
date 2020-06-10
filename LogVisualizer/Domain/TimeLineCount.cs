using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LogVisualizer.Domain
{

    public struct TimeLineCount
    {
        public TimeLineCount(DateTime date, int count, long offset)
        {
            Date = date;
            Count = count;
            Offset = offset;
        }

        public DateTime Date { get; }
        public int Count { get; }
        public long Offset { get; }

        public static readonly TimeLineCount Empty = new TimeLineCount(DateTime.MinValue, 0, 0);
    }
}
