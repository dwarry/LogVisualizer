using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LogVisualizer.Domain;
using LogVisualizer.ViewModels;
using MahApps.Metro.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LogVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IViewFor<MainWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(MainWindowViewModel), typeof(MainWindowViewModel), typeof(MainWindow));

        private DateTimeAxis _xAxis = new DateTimeAxis();

        private LinearAxis _yAxis = new LinearAxis();
        
        private LinearBarSeries _series = new LinearBarSeries
        {
            Title = "Events per ",
            StrokeColor = OxyColors.Teal,
            FillColor = OxyColors.MediumTurquoise,
            BarWidth = 10,
            SelectionMode = OxyPlot.SelectionMode.Single,
            Selectable = true,
            StrokeThickness = 4,
        };

        public MainWindow()
        {
            InitializeComponent();

            InitializePlotModel();

            DataContext = ViewModel;

            this.WhenActivated(d => {
                var mouseDown = Observable.FromEventPattern<OxyMouseDownEventArgs>(_series, nameof(MouseDown));

                mouseDown
                    .Where(x => x.EventArgs.ClickCount == 1)
                    .Select(x => Axis.InverseTransform(x.EventArgs.Position, _xAxis, _yAxis).X)
                    .Subscribe(SelectClosestPoint)
                    .DisposeWith(d);

                mouseDown
                    .Where(x => x.EventArgs.ClickCount == 2)
                    .InvokeCommand(ViewModel.ZoomIn)
                    .DisposeWith(d);

                //var seriesSelectionChanged = Observable.FromEventPattern<EventArgs>(_series, nameof(_series.SelectionChanged));

                //seriesSelectionChanged
                //    .Subscribe(x => SetViewModelSelectedTimestamp())
                //    .DisposeWith(d);
            });
        }

        private void SelectClosestPoint(double x)
        {
            var selectedIndex = _series.Points.Count - 1;

            for(int i = 0; i < _series.Points.Count; ++i)
            {
                var p = _series.Points[i];
                if (p.X > x)
                {
                    if (i == 0)
                    {
                        selectedIndex = 0;
                        break;
                    }
                    var prevPoint = _series.Points[i - 1];
                    selectedIndex = (p.X - x) <= (x - prevPoint.X) ? i : i - 1;
                    break;
                }
            }

            _series.SelectItem(selectedIndex);

            var selectedX = _series.Points[selectedIndex].X;
            var ts = DateTimeAxis.ToDateTime(selectedX);
            ViewModel.SelectedTimestamp = ts;
        }



        private void InitializePlotModel()
        {
            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);
            PlotModel.Series.Add(_series);
        }

        public PlotModel PlotModel { get; } = new PlotModel
        {
            Title = "Events",
            SelectionColor = OxyColors.Red
            
        };

        private static readonly Dictionary<TimeLineTreeLevel, (string, DateTimeIntervalType)> XAxisSettingsByLevel =
            new Dictionary<TimeLineTreeLevel, (string, DateTimeIntervalType)> { 
                [TimeLineTreeLevel.Year] = ("yyyy", DateTimeIntervalType.Years),
                [TimeLineTreeLevel.Month] = ("yyyy-MM", DateTimeIntervalType.Months),
                [TimeLineTreeLevel.Day] = ("yyyy-MM-dd", DateTimeIntervalType.Days),
                [TimeLineTreeLevel.Hour] = ("HH", DateTimeIntervalType.Hours),
                [TimeLineTreeLevel.Minute] = ("HH:mm", DateTimeIntervalType.Minutes),
                [TimeLineTreeLevel.Second] = ("HH:mm:ss", DateTimeIntervalType.Seconds),
                [TimeLineTreeLevel.Millisecond] = ("ss.fff", DateTimeIntervalType.Milliseconds)
            };

        private void SetupSeries()
        {
            var (minDate, maxDate) = ViewModel.DateRange;

            var (format, intervalType) = XAxisSettingsByLevel[ViewModel.CurrentLevel];


            _xAxis.Minimum = DateTimeAxis.ToDouble(minDate);

            _xAxis.Maximum = DateTimeAxis.ToDouble(maxDate);

            _xAxis.StringFormat = format;

            _xAxis.IntervalType = intervalType;

            _series.Title = "Events per " + ViewModel.CurrentLevel.ToString().ToLowerInvariant();

            _series.ClearSelection();

            _series.Points.Clear();

            _series.Points.AddRange(ViewModel.Data.Select(x => DateTimeAxis.CreateDataPoint(x.Date, x.Count)));

            PlotModel.InvalidatePlot(true);
        }


        public MainWindowViewModel ViewModel
        {
            get => (MainWindowViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel {
            get => ViewModel;
            set => ViewModel = (MainWindowViewModel)value;
        }
    }
}
