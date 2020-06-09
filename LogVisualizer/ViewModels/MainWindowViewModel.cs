using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LogVisualizer.DialogServices;
using LogVisualizer.Domain;
using LogVisualizer.LogFileParsers;
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
        private readonly IDialogServices _dialogServices;
        
        private readonly TreeBuilderFactory _treeBuilderFactory;

        public MainWindowViewModel(IDialogServices dialogServices, TreeBuilderFactory treeBuilderFactory)
        {
            if (dialogServices == null)
            {
                throw new ArgumentNullException(nameof(dialogServices));
            }

            if (treeBuilderFactory == null)
            {
                throw new ArgumentNullException(nameof(treeBuilderFactory));
            }

            _dialogServices = dialogServices;
            
            _treeBuilderFactory = treeBuilderFactory;

            DateTime? currentTimestamp = null;

            this.WhenAnyValue(x => x.CurrentLevel,
                              x=>x.Tree,
                              (lvl, t) => (t != null ? t.CountsAtLevel(lvl, currentTimestamp) 
                                                     : Enumerable.Empty<TimeLineCount>())
                                                     .ToList()
                                                     .AsReadOnly())
                .ToPropertyEx(this, x => x.Data);

            OpenLogFile = ReactiveCommand.Create(SelectAndOpenLogFile);


            var canZoomIn = this.WhenAnyValue(x => x.CurrentLevel, x => x.SelectedTimestamp,
                                             (lvl, ts) => lvl != TimeLineTreeLevel.Millisecond
                                                          && ts.HasValue)
                .DistinctUntilChanged();

            ZoomIn = ReactiveCommand.Create(
                () =>
                {
                    currentTimestamp = SelectedTimestamp;
                    CurrentLevel = CurrentLevel.NextLevel();
                    SelectedTimestamp = null;
                },
                canZoomIn);


            var canZoomOut = this.WhenAnyValue(x => x.CurrentLevel,
                                               lvl => lvl != TimeLineTreeLevel.Year)
                .DistinctUntilChanged();

            ZoomOut = ReactiveCommand.Create(
                () =>
                {

                    CurrentLevel = CurrentLevel.PreviousLevel();
                    SelectedTimestamp = null;
                },
                canZoomOut);

            this.WhenAnyValue(x => x.Data)
                .Select(DeriveDateRange)
                .ToPropertyEx(this, x => x.DateRange);
        }

        private void SelectAndOpenLogFile()
        {
            var path = _dialogServices.ShowOpenFileDialog();

            if(path != null)
            {
                IsBusy = true;
                try
                {
                    var treeBuilder = _treeBuilderFactory.CreateTreeBuilderForLogFile(path);

                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        Tree = treeBuilder.ParseDataAndBuildTree(stream);
                        CurrentLevel = TimeLineTreeLevel.Year;
                        SelectedTimestamp = null;
                    }

                }
                finally
                {
                    IsBusy = false;
                }
            }
        }


        private Tuple<DateTime, DateTime> DeriveDateRange(IReadOnlyCollection<TimeLineCount> data)
        {
            DateTime firstTimestamp, lastTimestamp;

            if (data.Count == 0)
            {
                firstTimestamp = DateTime.UtcNow;

                lastTimestamp = firstTimestamp;
            }
            else
            {

                firstTimestamp = data.First().Date;

                lastTimestamp = data.Last().Date;
            }

            DateTime minDate, maxDate;

            switch (CurrentLevel)
            {
                case TimeLineTreeLevel.Year:
                    minDate = firstTimestamp.AddYears(-1);
                    maxDate = lastTimestamp.AddYears(1);
                    break;
                case TimeLineTreeLevel.Month:
                    minDate = new DateTime(firstTimestamp.Year, 1, 1);
                    maxDate = new DateTime(lastTimestamp.Year, 12, 31);
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
        public bool IsBusy { get; private set; }

        [Reactive]
        private TimelineTree Tree { get; set; }


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


        public ReactiveCommand<Unit, Unit> OpenLogFile { get; }

        public ReactiveCommand<Unit, Unit> ZoomIn { get; }

        public ReactiveCommand<Unit, Unit> ZoomOut { get; }
    }
}
