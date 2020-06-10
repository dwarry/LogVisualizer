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
using LogVisualizer.DialogServices;
using LogVisualizer.LogFileParsers;
using System.Reactive;
using System.Collections;

namespace LogVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IViewFor<MainWindowViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MainWindowViewModel), typeof(MainWindow));

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

        private LinearBarSeries _selection = new LinearBarSeries
        {
            RenderInLegend = false,
            StrokeColor = OxyColors.Red,
            FillColor = OxyColors.DarkRed,
            BarWidth = 10,
            Selectable = false,
            StrokeThickness = 4
        };

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainWindowViewModel(new WpfDialogServices(), new LogParserFactory());

            DataContext = ViewModel;

            InitializePlotModel();

            this.WhenActivated(d => {
                var mouseDown = Observable.FromEventPattern<OxyMouseDownEventArgs>(_series, nameof(MouseDown));

                mouseDown
                    .Where(x => x.EventArgs.ClickCount == 1)
                    .Select(x => x.EventArgs.Position)
                    .Subscribe(SelectClosestPoint)
                    .DisposeWith(d);

                mouseDown
                    .Where(x => x.EventArgs.ClickCount == 2)
                    .Select(args => Unit.Default)
                    .InvokeCommand(this, x => x.ViewModel.ZoomIn)
                    .DisposeWith(d);

                this.BindCommand(this.ViewModel, 
                    vm => vm.OpenLogFile, 
                    v => v.FileOpen);

                
                this.Bind(ViewModel, vm => (IEnumerable)vm.Lines, v => v.LogItems.ItemsSource);

                this.BindCommand(this.ViewModel, vm => vm.ZoomIn, vw => vw.ZoomIn);
                this.BindCommand(this.ViewModel, vm => vm.ZoomOut, vw => vw.ZoomOut);

                ViewModel.WhenAnyValue(x => x.DateRange).Subscribe(_ => SetupSeries()).DisposeWith(d);
            });
        }

        private void SelectClosestPoint(ScreenPoint p)
        {
            var thr = _series.GetNearestPoint(p, false);

            if(thr != null)
            {
                var ts = DateTimeAxis.ToDateTime(thr.DataPoint.X);
                ViewModel.SelectedTimestamp = ts;
                _selection.Points.Clear();
                _selection.Points.Add(thr.DataPoint);
                PlotModel.InvalidatePlot(false);
            }
        }



        private void InitializePlotModel()
        {
            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);
            PlotModel.Series.Add(_series);
            PlotModel.Series.Add(_selection);

            TimeLine.Model = PlotModel;
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

            _selection.Points.Clear();

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
