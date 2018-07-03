using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class CPUAndMemoryChartSample
    {
        public static void Run()
        {
            var app = new ConsoleApp(0, 0, 75, 22);
            var chart = new CpuAndMemoryChart();
            var list = new Grid() { Size = new Size(30, 5) };

            var source = new MemoryDataSource();
            list.DataSource = source;

            Action syncChartToListAction = () =>
            {
                source.Items.Clear();
                if (chart.ViewModel.FocusedDataSeries != null && chart.ViewModel.FocusedDataPointIndex >= 0 && chart.ViewModel.FocusedDataPointIndex < chart.ViewModel.FocusedDataSeries.DataPoints.Count && chart.ViewModel.FocusedDataSeries.DataPoints.Count > 0)
                {
                    source.Items.Add(new { Value = ContextAssistSearchResult.FromString(chart.ViewModel.FocusedDataSeries.DataPoints[chart.ViewModel.FocusedDataPointIndex].Y + "") });
                }
            };

            chart.ViewModel.FocusedDataPointChanged += syncChartToListAction;
            chart.ViewModel.FocusedSeriesChanged +=  syncChartToListAction;
            syncChartToListAction();

            app.LayoutRoot.Controls.Add(chart);
            app.LayoutRoot.Controls.Add(list);
            app.Start();
        }
    }

    public class CpuAndMemoryChart : LineChart
    {
        public CpuAndMemoryChart()
        {
            InitSeries();
            InitAxes();
            AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this);
        }

        private void InitAxes()
        {
            // customizes the amount to indent the Y axis.  You can do this based on typical y axis labels for your
            // scenario.
            YAxisLeftOffset = 8;

            // CPU values go from 0 to 100
            ViewModel.YMinimumOverride = 0;
            ViewModel.YMaximumOverride = 100;

            // y axis will show the past minute
            var now = DateTime.Now;
            ViewModel.XMinimumOverride = now.Ticks - TimeSpan.FromMinutes(1).Ticks;
            ViewModel.XMaximumOverride = now.Ticks;

            // show y labels that are appropriate for second granularity, since we'll add a new point every second
            XAxisValueFormatter = ChartLabelFormatters.SecondGranularityTimestamp;
            XAxisValueCompactFormatter = ChartLabelFormatters.SecondGranularityCompactTimestamp;

            // for the x values we can explicityly format it as a percentage
            YAxisValueCompactFormatter = (d) => new ConsoleString(Math.Round(d, 1) + " %");
            YAxisValueFormatter = YAxisValueCompactFormatter;
        }

        private void InitSeries()
        {
            var cpuSeries = new DataSeries();
            cpuSeries.Title = "CPU %";
            cpuSeries.PlotColor = ConsoleColor.DarkGreen;
            cpuSeries.PlotCharacter = 'c';
            cpuSeries.ShowAreaUnderEachDataPoint = true;

            // optionally define a threshold for a series
            cpuSeries.Threshold = new Threshold() { Value = 40, Title = "CPU Warning threshold", Type = ThresholdType.Maximum, PlotColor = ConsoleColor.DarkGreen };

            var memSeries = new DataSeries();
            memSeries.Title = "Memory %";
            memSeries.PlotColor = ConsoleColor.DarkMagenta;
            memSeries.PlotCharacter = 'm';
            memSeries.Threshold = new Threshold() { Value = 80, Title = "Memory Warning threshold" , Type = ThresholdType.Maximum, PlotColor = ConsoleColor.DarkMagenta};

            ViewModel.DataSeriesCollection.Add(memSeries);
            ViewModel.DataSeriesCollection.Add(cpuSeries);        
        }

        private void OnAddedToVisualTree()
        {
            if (Width == 0) Width = Parent.Width;
            if (Height == 0) Height = Parent.Height;

            // start monitoring CPU and memory when added
            Task.Factory.StartNew(() => { WatchCPUAndMemory(); });
        }

        private void WatchCPUAndMemory()
        {
            var running = true;

            // Stop watching CPU and memory when the application stops or this control is removed
            Application.Stopped.SubscribeForLifetime(() => { running = false; }, Application);
            this.RemovedFromVisualTree.SubscribeForLifetime(() => { running = false; }, this);

            while (running)
            {
                var now = DateTime.Now;

                var cpuUsed = PerformanceInfo.GetCPUPercentage();
                var memUsed = Math.Round(100 - (100.0 * PerformanceInfo.GetPhysicalAvailableMemoryInMiB() / PerformanceInfo.GetTotalMemoryInMiB()), 1);

                // slide the window so it always shows the last minute
                ViewModel.XMaximumOverride = now.Ticks;
                ViewModel.XMinimumOverride = now.Ticks - TimeSpan.FromMinutes(1).Ticks;

                // add the latest value to the series
                ViewModel.DataSeriesCollection[0].DataPoints.Add(new DataPoint() { X = now.Ticks, Y = cpuUsed });
                ViewModel.DataSeriesCollection[1].DataPoints.Add(new DataPoint() { X = now.Ticks, Y = memUsed });

                // Remove the oldest data point if we have a minute worth of data on the chart
                if (ViewModel.DataSeriesCollection[0].DataPoints.Count > 60)
                {
                    ViewModel.DataSeriesCollection[0].DataPoints.RemoveAt(0);
                    ViewModel.DataSeriesCollection[1].DataPoints.RemoveAt(0);
                }
                Thread.Sleep(1000);
            }
        }
    }
}