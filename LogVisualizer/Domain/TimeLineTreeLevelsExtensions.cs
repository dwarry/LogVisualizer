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

        public static bool HasNextLevel(this TimeLineTreeLevel self) => self != TimeLineTreeLevel.Millisecond;

        public static bool HasPrevLevel(this TimeLineTreeLevel self) => self != TimeLineTreeLevel.Year;

        public static TimeLineTreeLevel NextLevel(this TimeLineTreeLevel self)
        {
            switch (self)
            {
                case TimeLineTreeLevel.Year:
                    return TimeLineTreeLevel.Month;

                case TimeLineTreeLevel.Month:
                    return TimeLineTreeLevel.Day;

                case TimeLineTreeLevel.Day:
                    return TimeLineTreeLevel.Hour;

                case TimeLineTreeLevel.Hour:
                    return TimeLineTreeLevel.Minute;

                case TimeLineTreeLevel.Minute:
                    return TimeLineTreeLevel.Second;

                case TimeLineTreeLevel.Second:
                    return TimeLineTreeLevel.Millisecond;

                default:
                    throw new ArgumentOutOfRangeException(nameof(self), "Millisecond is the lowest level");
            }
        }

        public static TimeLineTreeLevel PreviousLevel(this TimeLineTreeLevel self)
        {
            switch (self)
            {
                case TimeLineTreeLevel.Month:
                    return TimeLineTreeLevel.Year;

                case TimeLineTreeLevel.Day:
                    return TimeLineTreeLevel.Month;

                case TimeLineTreeLevel.Hour:
                    return TimeLineTreeLevel.Day;

                case TimeLineTreeLevel.Minute:
                    return TimeLineTreeLevel.Hour;

                case TimeLineTreeLevel.Second:
                    return TimeLineTreeLevel.Minute;
                    
                case TimeLineTreeLevel.Millisecond:
                    return TimeLineTreeLevel.Second;
                   
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), "No previous value");
            }
        }
        
    }
}
