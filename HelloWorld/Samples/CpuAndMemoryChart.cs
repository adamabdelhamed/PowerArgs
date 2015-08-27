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
            var list = new ListView() { Size = new Size(30, 5) };

            Action syncChartToListAction = () =>
            {
                list.ViewModel.Items.Clear();
                if (chart.ViewModel.FocusedDataSeries != null && chart.ViewModel.FocusedDataPointIndex >= 0 && chart.ViewModel.FocusedDataPointIndex < chart.ViewModel.FocusedDataSeries.DataPoints.Count && chart.ViewModel.FocusedDataSeries.DataPoints.Count > 0)
                {
                    list.ViewModel.Items.Add(ContextAssistSearchResult.FromString(chart.ViewModel.FocusedDataSeries.DataPoints[chart.ViewModel.FocusedDataPointIndex].Y + ""));
                }
            };

            chart.ViewModel.FocusedDataPointChanged += syncChartToListAction;
            chart.ViewModel.FocusedSeriesChanged +=  syncChartToListAction;
            syncChartToListAction();

            app.Controls.Add(chart);
            app.Controls.Add(list);
            app.Run();
        }
    }

    public class CpuAndMemoryChart : LineChart
    {
        public CpuAndMemoryChart()
        {
            InitSeries();
            InitAxes();
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

        public override void OnAdd(ConsoleControl parent)
        {
            base.OnAdd(parent);
            if (Width == 0) Width = parent.Width;
            if (Height == 0) Height = parent.Height;

            // start monitoring CPU and memory when added
            Task.Factory.StartNew(() => { WatchCPUAndMemory(); });
        }

        private void WatchCPUAndMemory()
        {
            var running = true;

            // Stop watching CPU and memory when the application stops or this control is removed
            Application.ApplicationStopped += () => { running = false; };
            this.Removed +=                   () => { running = false; };

            while (running)
            {
                // the disposable lock ensures that the app only repaints one time when the lock is disposed.
                // without this, the app can repaint twice, once for the new point we add, and once for the
                // point we remove.  That might not be too bad, but this pattern works well if you're making
                // many changes in the background and don't want to repaint for each change.  Note that this
                // doesn't throttle the OnPaint() method of controls, it only throttles the underlying bitmap
                // from actually rendering itself on the console.
                using (Application.GetDisposableLock())
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
                }

                Thread.Sleep(1000);
            }
        }
    }
}