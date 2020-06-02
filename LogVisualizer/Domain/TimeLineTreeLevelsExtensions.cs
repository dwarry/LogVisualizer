using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LogVisualizer.Domain
{

    internal static class TimeLineTreeLevelsExtensions
    {
        public static short PartOf(this TimeLineTreeLevel self, DateTime timestamp)
        {
            switch (self)
            {
                case TimeLineTreeLevel.Year: return (short)timestamp.Year;
                case TimeLineTreeLevel.Month: return (short)timestamp.Month;
                case TimeLineTreeLevel.Day: return (short)timestamp.Day;
                case TimeLineTreeLevel.Hour: return (short)timestamp.Hour;
                case TimeLineTreeLevel.Minute: return (short)timestamp.Minute;
                case TimeLineTreeLevel.Second: return (short)timestamp.Second;
                case TimeLineTreeLevel.Millisecond: return (short)timestamp.Millisecond;
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), SR.Error_unknown_value);
            }
        }
    }
}
