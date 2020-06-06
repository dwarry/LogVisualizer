using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogVisualizer.Domain;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;
using ReactiveUI.Fody;
using ReactiveUI.Fody.Helpers;

namespace LogVisualizer.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {

        private TimelineTree _tree;

        public MainWindowViewModel()
        {
            DateTime? currentTimestamp = null;

            this.WhenAnyValue(x => x.CurrentLevel,
                              lvl => _tree.CountsAtLevel(lvl, currentTimestamp).ToList().AsReadOnly())
                .ToPropertyEx(this, x => x.Data);

            var canZoomIn = this.WhenAnyValue(x => x.CurrentLevel,
                                             lvl => lvl != TimeLineTreeLevel.Millisecond
                                                    && SelectedTimestamp.HasValue);

            ZoomIn = ReactiveCommand.Create(
                () =>
                {
                    currentTimestamp = SelectedTimestamp;
                    CurrentLevel = CurrentLevel.NextLevel();
                },
                canZoomIn);


            var canZoomOut = this.WhenAnyValue(x => x.CurrentLevel,
                              lvl => lvl != TimeLineTreeLevel.Year);

            ZoomOut = ReactiveCommand.Create(
                () =>
                {

                    CurrentLevel = CurrentLevel.PreviousLevel();
                },
                canZoomOut);

            this.WhenAnyValue(x => x.Data)
                .Select(DeriveDateRange)
                .ToPropertyEx(this, x => x.DateRange);
        }


        private Tuple<DateTime, DateTime> DeriveDateRange(IReadOnlyCollection<TimeLineCount> data)
        {
            var firstTimestamp = data.FirstOrDefault()?.Date ?? DateTime.UtcNow;

            var lastTimestamp = data.LastOrDefault()?.Date ?? DateTime.UtcNow;

            DateTime minDate, maxDate;


            switch (CurrentLevel)
            {
                case TimeLineTreeLevel.Year:
                    minDate = firstTimestamp.AddYears(-1);
                    maxDate = lastTimestamp.AddYears(1);
                    break;
                case TimeLineTreeLevel.Month:
                    minDate = new DateTime(firstTimestamp.Year - 1, 12, 1);
                    maxDate = new DateTime(lastTimestamp.Year + 1, 1, 1);
                    break;

                case TimeLineTreeLevel.Day:
                    minDate = new DateTime(firstTimestamp.Year, firstTimestamp.Month, 1).AddDays(-1);
                    maxDate = new DateTime(lastTimestamp.Year, lastTimestamp.Month, 1).AddMonths(1).AddDays(-1);
                    break;

                case TimeLineTreeLevel.Hour:
                    minDate = new DateTime(firstTimestamp.Year, firstTimestamp.Month, firstTimestamp.Day, 0, 0, 0);
                    maxDate = new DateTime(lastTimestamp.Year, lastTimestamp.Month, lastTimestamp.Day, 0, 0, 0).AddDays(1);
                    break;

                case TimeLineTreeLevel.Minute:
                    minDate = new DateTime(firstTimestamp.Year, firstTimestamp.Month, firstTimestamp.Day, firstTimestamp.Hour, 0, 0);
                    maxDate = minDate.AddHours(1);
                    break;

                case TimeLineTreeLevel.Second:
                    minDate = new DateTime(firstTimestamp.Year, firstTimestamp.Month, firstTimestamp.Day, firstTimestamp.Hour, firstTimestamp.Minute, 0);
                    maxDate = minDate.AddMinutes(1);
                    break;

                case TimeLineTreeLevel.Millisecond:
                    minDate = new DateTime(firstTimestamp.Year, firstTimestamp.Month, firstTimestamp.Day, firstTimestamp.Hour, firstTimestamp.Minute, firstTimestamp.Second);
                    maxDate = minDate.AddSeconds(1);
                    break;

                default:
                    throw new InvalidOperationException(SR.Error_unknown_value + CurrentLevel.ToString());
            }

            return Tuple.Create(minDate, maxDate);
        }


        [Reactive]
        public TimeLineTreeLevel CurrentLevel
        {
            get; set;
        }


        [Reactive]
        public DateTime? SelectedTimestamp
        {
            get; set;
        }


        public extern IReadOnlyCollection<TimeLineCount> Data { [ObservableAsProperty] get; }


        public extern Tuple<DateTime, DateTime> DateRange { [ObservableAsProperty] get; }


        public ReactiveCommand<Unit, Unit> ZoomIn { get; }

        public ReactiveCommand<Unit, Unit> ZoomOut { get; }
    }
}
