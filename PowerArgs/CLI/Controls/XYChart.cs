using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// The options for the XYChart control
    /// </summary>
    public class XYChartOptions
    {
        /// <summary>
        /// Sets a title for the chart
        /// </summary>
        public ConsoleString Title { get; set; } = "Chart1".ToConsoleString();

        /// <summary>
        /// The data to plot
        /// </summary>
        public List<Series> Data { get; set; }

        /// <summary>
        /// When specified, forces the bottom of the Y axis to this value rather than
        /// letting the chart choose an appropriate value
        /// </summary>
        public double? YMinOverride { get; set; }

        /// <summary>
        /// When specified, forces the top of the Y axis to this value rather than
        /// letting the chart choose an appropriate value
        /// </summary>
        public double? YMaxOverride { get; set; }

        /// <summary>
        /// When specified, forces the left of the X axis to this value rather than
        /// letting the chart choose an appropriate value
        /// </summary>
        public double? XMinOverride { get; set; }

        /// <summary>
        /// When specified, forces the right of the X axis to this value rather than
        /// letting the chart choose an appropriate value
        /// </summary>
        public double? XMaxOverride { get; set; }

        /// <summary>
        /// Sets the formatter of the x axis. The default is to use a number formatter.
        /// </summary>
        public IAxisFormatter XAxisFormatter { get; set; } = new NumberFormatter();

        /// <summary>
        /// Sets the formatter of the y axis. The default is to use a number formatter.
        /// </summary>
        public IAxisFormatter YAxisFormatter { get; set; } = new NumberFormatter();

        /// <summary>
        /// When you let the chart determine appropriate x axis boundaries it will pad 
        /// the data in the values by this value, as a percentage of the range. 
        /// </summary>
        public double XAxisRangePadding { get; set; } = .15;

        /// <summary>
        /// When you let the chart determine appropriate y axis boundaries then it will pad 
        /// the data in the values by this value, as a percentage of the range. 
        /// </summary>
        public double YAxisRangePadding { get; set; } = .25;
    }

    /// <summary>
    /// A model for a series of data that can be plotted on an XYChart
    /// </summary>
    public class Series
    {
        /// <summary>
        /// The title of the series
        /// </summary>
        public string Title { get; set; } = "Series1";

        /// <summary>
        /// The character to use to visually represent a point in this data series
        /// </summary>
        public ConsoleCharacter PlotCharacter { get; set; } = new ConsoleCharacter('X', ConsoleColor.White);

        /// <summary>
        /// When set to true the user will be able to focus on the data points in this series using the tab and arrow keys.
        /// </summary>
        public bool AllowInteractivity { get; set; } = true;

        /// <summary>
        /// The data points in this series
        /// </summary>
        public List<DataPoint> Points { get; set; }
    }

    /// <summary>
    /// A model for a data point that can be plotted on an XYChart
    /// </summary>
    public class DataPoint
    {
        /// <summary>
        /// The x coordinate
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// The y coordinate
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// A chart control that can render 2d data points on an X and Y axis
    /// </summary>
    public class XYChart : ConsolePanel
    {
        private class AxisLabelInfo
        {
            public ConsoleString Label { get; set; }
            public double Value { get; set; }
        }

        private class DataPointControl : PixelControl
        {
            public DataPoint DataPoint { get; set; }
            public Series Series { get; set; }
        }

        private const int MaxXAxisLabelLength = 10;
        private const int XAxisBottomOffset = 2;
        private const int YAxisTop = 0;

        private int maxYAxisLabelLength; // dynamically calculated based on the y axis label lengths.
        private int XAxisYValue => this.Height - XAxisBottomOffset;
        private int XAxisRight => Width - 1;
        private int XAxisLeft => maxYAxisLabelLength;
        private int YAxisBottom => XAxisYValue;
        private int XAxisWidth => XAxisRight - XAxisLeft;
        private int YAxisHeight => YAxisBottom - YAxisTop;

        private double MinXValueInPlotArea
        {
            get
            {
                if (options.XMinOverride.HasValue)
                {
                    return options.XMinOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.X).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.X).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.XAxisRangePadding;
                return trueMin - padding / 2.0;
            }
        }
       

        private double MinYValueInPlotArea
        {
            get
            {
                if (options.YMinOverride.HasValue)
                {
                    return options.YMinOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.Y).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.Y).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.YAxisRangePadding;
                return trueMin - padding / 2.0;
            }
        }

        private double MaxXValueInPlotArea
        {
            get
            {
                if (options.XMaxOverride.HasValue)
                {
                    return options.XMaxOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.X).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.X).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.XAxisRangePadding;
                return trueMax + padding / 2.0;
            }
        }


        private double MaxYValueInPlotArea
        {
            get
            {
                if (options.YMaxOverride.HasValue)
                {
                    return options.YMaxOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.Y).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.Y).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.YAxisRangePadding;
                return trueMax + padding / 2.0;
            }
        }

        private Label chartTitleLabel;
        private Label seriesTitleLabel;

        private XYChartOptions options;

        /// <summary>
        /// Initializes the chart with the given options
        /// </summary>
        /// <param name="options">the options to use to render the chart</param>
        public XYChart(XYChartOptions options)
        {
            this.options = options;
            AddDataPoints();
            this.SubscribeForLifetime(nameof(Bounds), PositionDataPoints, this);
            chartTitleLabel = Add(new Label() { Text = options.Title }).CenterHorizontally().DockToTop(padding: 2);
            seriesTitleLabel = Add(new Label() { Text = "Series1".ToConsoleString() }).CenterHorizontally().DockToTop(padding: 3);

            this.AddedToVisualTree.SubscribeOnce(() =>
            {
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, HandleUpArrow, this);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, HandleDownArrow, this);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.LeftArrow, null, HandleLeftArrow, this);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, HandleRightArrow, this);
            });
        }

        /// <summary>
        /// Shows the data in a chart via an interactive console app
        /// </summary>
        /// <param name="data">the data points, where each inner array is expected to have 2 values, the first being the X value and the second being the Y value</param>
        public static void Show(IEnumerable<double[]> data) => Show(data.Select(d => new DataPoint() { X = d[0], Y = d[1] }));

        /// <summary>
        /// Shows the data in a chart via an interactive console app
        /// </summary>
        /// <param name="data">the data points, where the first item in the tuple is the X value and the second is the Y value</param>
        public static void Show(IEnumerable<Tuple<double, double>> data) => Show(data.Select(d => new DataPoint() { X = d.Item1, Y = d.Item2 }));

        /// <summary>
        /// Shows the data in a chart via an interactive console app
        /// </summary>
        /// <param name="data">the data points to plot</param>
        public static void Show(IEnumerable<DataPoint> data) => Show(new XYChartOptions() { Data = new List<Series>() { new Series(){ Points = data.ToList() } } });

        /// <summary>
        /// Shows the data in a chart via an interactive console app
        /// </summary>
        /// <param name="options">options used to render the chart</param>
        public static void Show(XYChartOptions options) => ConsoleApp.Show(new XYChart(options));

        /// <summary>
        /// Re-evaluates the data and re-renders the chart
        /// </summary>
        public void Refresh()
        {
            AddDataPoints();
            PositionDataPoints();
        }

        /// <summary>
        /// Paints the chart
        /// </summary>
        /// <param name="context">the pain context</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            RenderYAxis(context); // MUST render y axis first because the variable y axis label widths cause the plot area to change
            PaintXAxis(context);
            base.OnPaint(context);
        }
        
        private void PaintXAxis(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter('-', Foreground, Background);
            context.DrawLine(XAxisLeft, XAxisYValue, XAxisRight, XAxisYValue); // horizontal line at the bottom of the x axis

            var minSpaceBetweenLabels = 1;
            var maxNumberOfLabels = XAxisWidth / (MaxXAxisLabelLength + minSpaceBetweenLabels);

            foreach (var labelValue in options.XAxisFormatter.GetOptimizedAxisLabelValues(MinXValueInPlotArea, MaxXValueInPlotArea, maxNumberOfLabels))
            {
                var label = options.XAxisFormatter.FormatValue(MinXValueInPlotArea, MaxXValueInPlotArea, labelValue);

                if (label.Length > MaxXAxisLabelLength)
                {
                    label = label.Substring(0, MaxXAxisLabelLength);
                }

                var x = ConvertXValueToPixel(labelValue);
                var y = XAxisYValue + 1;

                if (x + label.Length <= Width)
                {
                    context.Pen = new ConsoleCharacter('^', Foreground, Background);
                    context.DrawPoint(x, y - 1);
                    context.DrawString(label, x, y);
                }
            }
        }

        /// <summary>
        /// This method has the side effect of updating the maxYAxisLabelLength, which is used by the various calculations
        /// that determine the plot area. It needs to be called anytime the data has changed, and this next part is important, it must
        /// be called as the very first part of the rendering pass since just about all of the rendering depends on the plot area being defined.
        /// </summary>
        /// <returns>the label info for the y axis</returns>
        private List<AxisLabelInfo> DetermineYAxisLabels()
        {
            var maxNumberOfLabels = YAxisHeight / 3;
            maxYAxisLabelLength = 0;
            var labels = new List<AxisLabelInfo>();
            foreach (var labelValue in options.YAxisFormatter.GetOptimizedAxisLabelValues(MinYValueInPlotArea, MaxYValueInPlotArea, maxNumberOfLabels))
            {
                var label = options.YAxisFormatter.FormatValue(MinYValueInPlotArea, MaxYValueInPlotArea, labelValue);
                labels.Add(new AxisLabelInfo() { Label = label, Value = labelValue });
                maxYAxisLabelLength = Math.Max(maxYAxisLabelLength, label.Length);
            }
            return labels;
        }

        private void RenderYAxis(ConsoleBitmap context)
        {
            var labels = DetermineYAxisLabels();

            context.Pen = new ConsoleCharacter('|', Foreground, Background);
            context.DrawLine(XAxisLeft, YAxisTop, XAxisLeft, YAxisBottom);

            foreach (var label in labels)
            {
                var x = maxYAxisLabelLength - label.Label.Length;
                var y = ConvertYValueToPixel(label.Value);
                context.DrawString(label.Label, x, y);
                context.Pen = new ConsoleCharacter('>', Foreground, Background);
                context.DrawPoint(maxYAxisLabelLength,y);
            }
        }

        private void AddDataPoints()
        {
            this.Controls.Clear();
            foreach (var series in options.Data)
            {
                for (int i = 0; i < series.Points.Count; i++)
                {
                    var pixel = new DataPointControl() { CanFocus = series.AllowInteractivity, DataPoint = series.Points[i], Series = series, Value = series.PlotCharacter };
                    pixel.Focused.SubscribeForLifetime(() =>
                    {
                        pixel.Value = new ConsoleCharacter(series.PlotCharacter.Value, ConsoleColor.Cyan);
                        var newTitle = pixel.Series.Title.ToConsoleString(pixel.Series.PlotCharacter.ForegroundColor);
                        var xValue = options.XAxisFormatter.FormatValue(MinXValueInPlotArea, MaxXValueInPlotArea, pixel.DataPoint.X);
                        var yValue = options.YAxisFormatter.FormatValue(MinYValueInPlotArea, MaxYValueInPlotArea, pixel.DataPoint.Y);
                        newTitle += new ConsoleString(" ( " + xValue + ", " + yValue + " )", series.PlotCharacter.ForegroundColor);
                        seriesTitleLabel.Text = newTitle;
                    }, pixel);
                    pixel.Unfocused.SubscribeForLifetime(() => pixel.Value = series.PlotCharacter, pixel);
                    this.Controls.Add(pixel);
                }
            }
        }

        private void PositionDataPoints()
        {
            DetermineYAxisLabels(); // ensures the y axis is offset properly due to variable label widths
            foreach (var control in Controls.Where(c => c is DataPointControl).Select(c => c as DataPointControl))
            {
                control.X = ConvertXValueToPixel(control.DataPoint.X);
                control.Y = ConvertYValueToPixel(control.DataPoint.Y);
            }
        }

        private void HandleUpArrow()
        {
            DataPointControl focusedPoint = Application.FocusManager.FocusedControl as DataPointControl;
            if (focusedPoint == null || Controls.Contains(focusedPoint) == false) return;

            Controls
                .Where(c => c is DataPointControl)
                .Where(p => p.Y < focusedPoint.Y)
                .OrderBy(p => CalculateDistanceBetween(focusedPoint.DataPoint, (p as DataPointControl).DataPoint))
                .FirstOrDefault()
                ?.TryFocus();
        }

        private void HandleDownArrow()
        {
            DataPointControl focusedPoint = Application.FocusManager.FocusedControl as DataPointControl;
            if (focusedPoint == null || Controls.Contains(focusedPoint) == false) return;

            Controls
                .Where(c => c is DataPointControl)
                .Where(p => p.Y > focusedPoint.Y)
                .OrderBy(p => CalculateDistanceBetween(focusedPoint.DataPoint, (p as DataPointControl).DataPoint))
                .FirstOrDefault()
                ?.TryFocus();
        }

        private void HandleLeftArrow()
        {
            DataPointControl focusedPoint = Application.FocusManager.FocusedControl as DataPointControl;
            if (focusedPoint == null || Controls.Contains(focusedPoint) == false) return;

            Controls
                .Where(c => c is DataPointControl)
                .Where(p => p.X < focusedPoint.X)
                .OrderBy(p => CalculateDistanceBetween(focusedPoint.DataPoint, (p as DataPointControl).DataPoint))
                .FirstOrDefault()
                ?.TryFocus();
        }

        private void HandleRightArrow()
        {
            DataPointControl focusedPoint = Application.FocusManager.FocusedControl as DataPointControl;
            if (focusedPoint == null || Controls.Contains(focusedPoint) == false) return;

            Controls
                .Where(c => c is DataPointControl)
                .Where(p => p.X > focusedPoint.X)
                .OrderBy(p => CalculateDistanceBetween(focusedPoint.DataPoint, (p as DataPointControl).DataPoint))
                .FirstOrDefault()
                ?.TryFocus();
        }
        
        private int ConvertXValueToPixel(double x)
        {
            double xRange = MaxXValueInPlotArea - MinXValueInPlotArea;
            double delta = x - MinXValueInPlotArea;
            double percentage = delta / xRange;

            if (percentage < 0 || percentage > 100)
            {
                return -1;
            }
            else
            {
                double xConverted = XAxisLeft + (XAxisWidth * percentage);
                return (int)Math.Round(xConverted);
            }
        }

        private double ConvertXPixelToValue(int x)
        {
            double delta = x - XAxisLeft;
            double percentage = delta / XAxisWidth;

            if (percentage < 0 || percentage > 100)
            {
                return -1;
            }
            else
            {
                double dataRange = MaxXValueInPlotArea - MinXValueInPlotArea;
                double xConverted = MinXValueInPlotArea + (dataRange * percentage);
                return xConverted;
            }
        }

        private int ConvertYValueToPixel(double y)
        {
            double yRange = MaxYValueInPlotArea - MinYValueInPlotArea;
            double delta = y - MinYValueInPlotArea;
            double percentage = delta / yRange;

            if (percentage < 0 || percentage > 100)
            {
                return -1;
            }
            else
            {
                double yConverted = YAxisBottom - (YAxisHeight * percentage);
                return (int)Math.Round(yConverted);
            }

        }

        private double ConvertYPixelToValue(int y)
        {
            double delta = YAxisBottom - y;
            double percentage = delta / YAxisHeight;

            if (percentage < 0 || percentage > 100)
            {
                return -1;
            }
            else
            {
                double dataRange = MaxYValueInPlotArea - MinYValueInPlotArea;
                double yConverted = MinYValueInPlotArea + (dataRange * percentage);
                return yConverted;
            }
        }

        private static double GetMaxValueAcrossSeries(IEnumerable<Series> seriesCollection, Func<Series, double> maxFunc)
        {
            double ret = 0;

            foreach (var series in seriesCollection)
            {
                ret = Math.Max(ret, maxFunc(series));
            }

            return ret;
        }

        private static double GetMinValueAcrossSeries(IEnumerable<Series> seriesCollection, Func<Series, double> minFunc)
        {
            double ret = double.MaxValue;

            foreach (var series in seriesCollection)
            {
                ret = Math.Min(ret, minFunc(series));
            }

            return ret;
        }

        private void PadZeroRangeIfNeeded(ref double min, ref double max)
        {
            if (min == max)
            {
                var valuePadding = Math.Abs(min) * .05;
                if (valuePadding == 0)
                {
                    valuePadding = 1;
                }
                var newMin = min - valuePadding;
                max = min + valuePadding;
                min = newMin;
            }
        }

        private double CalculateDistanceBetween(DataPoint a, DataPoint b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }

    /// <summary>
    /// The interface for an axis formatter
    /// </summary>
    public interface IAxisFormatter
    {
        /// <summary>
        /// Gets the values for ideal increments to be shown on an axis
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="maxNumberOfLabels">the maximun number of labels that can be rendered on the axis</param>
        /// <returns>the ideal increments</returns>
        List<double> GetOptimizedAxisLabelValues(double min, double max, int maxNumberOfLabels);

        /// <summary>
        /// Formats the given value for display
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="value">the value to format</param>
        /// <returns>the formatted value</returns>
        ConsoleString FormatValue(double min, double max, double value);
    }

    /// <summary>
    /// A general purpose formatter used to format a number axis
    /// </summary>
    public class NumberFormatter : IAxisFormatter
    {
        /// <summary>
        /// Gets the values for ideal increments to be shown on an axis
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="maxNumberOfLabels">the maximun number of labels that can be rendered on the axis</param>
        /// <returns></returns>
        public List<double> GetOptimizedAxisLabelValues(double min, double max, int maxNumberOfLabels)
        {
            double xRange = max - min;

            // these increments and all power of 10 variants result in visually pleasant labels 
            var incrementBases = new double[] { 1, 2, 5, 10, 25 };
            var rangeToMaxLabelRatio = xRange / maxNumberOfLabels;

            // while the scale of the range is too big for the biggest increment,
            // increase the increment set by an order of magnitude
            while (rangeToMaxLabelRatio > incrementBases[incrementBases.Length - 1])
            {
                for (var i = 0; i < incrementBases.Length; i++)
                {
                    incrementBases[i] = incrementBases[i] * 10;
                }
            }

            // while the scale of the range is too small for the smallest increment,
            // decrease the increment set by an order of magnitude
            while (rangeToMaxLabelRatio < incrementBases[0])
            {
                for (var i = 0; i < incrementBases.Length; i++)
                {
                    incrementBases[i] = incrementBases[i] / 10;
                }
            }

            // Now that we are in the right scale for our range, create labels for all the
            // increments and pick the one that gets closest to our max number of labels
            // without going over.

            var bestFitLabelsIncrement = incrementBases.OrderBy((increment) =>
            {
                var labels = GetLabels(min, max, increment);
                if (labels.Count > maxNumberOfLabels)
                {
                    return int.MaxValue;
                }
                else
                {
                    return maxNumberOfLabels - labels.Count;
                }
            }).First();

            return GetLabels(min, max, bestFitLabelsIncrement);
        }

        /// <summary>
        /// Formats the given value for display
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="value">the value to format</param>
        /// <returns>the formatted value</returns>
        public ConsoleString FormatValue(double min, double max, double value)
        {
            if (value == Math.Round(value))
            {
                return String.Format("{0:n0}", value).ToConsoleString();
            }
            else
            {
                return value.ToString().ToConsoleString();
            }
        }

        private List<double> GetLabels(double minX, double maxX, double increment)
        {
            var ret = new List<double>();
            var currentLabelValue = minX - (minX % increment);
            while (currentLabelValue <= maxX)
            {
                ret.Add(currentLabelValue);
                currentLabelValue += increment;
            }
            return ret;
        }
    }


    /// <summary>
    /// A formatter that can format date times on an axis
    /// </summary>
    public class DateTimeFormatter : IAxisFormatter
    {
        const int MaxLabelsOnAnyChartAssumption = 50;

        /// <summary>
        /// Gets the values for ideal increments to be shown on an axis
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="maxNumberOfLabels">the maximun number of labels that can be rendered on the axis</param>
        /// <returns>the ideal increments</returns>
        public List<double> GetOptimizedAxisLabelValues(double min, double max, int maxNumberOfLabels)
        {
            var minX = new DateTime((long)min);
            var maxX = new DateTime((long)max);
            var xRange = maxX - minX;

            // For really large time spans just use years and fall back to a base 10 incremental scale strategy
            if (xRange.TotalDays > 365 * MaxLabelsOnAnyChartAssumption)
            {
                return new NumberFormatter().GetOptimizedAxisLabelValues(minX.Year, maxX.Year + 1, maxNumberOfLabels)
                    .Select(d => (double)new DateTime((int)d, 1, 1).Ticks)
                    .ToList();
            }

            // For all other time spans these are the candidates for pleasant axis increments
            var incrementorCandidates = new List<TimeIncrementor>();
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.000025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.01)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.05)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.25)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(15)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(30)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(15)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(30)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(6)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(12)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(2)));
            incrementorCandidates.Add(new HalfMonthIncrementor());
            incrementorCandidates.Add(new MonthIncrementor(1));
            incrementorCandidates.Add(new MonthIncrementor(2));
            incrementorCandidates.Add(new MonthIncrementor(3));
            incrementorCandidates.Add(new MonthIncrementor(6));
            incrementorCandidates.Add(new YearIncrementor(1));
            incrementorCandidates.Add(new YearIncrementor(2));
            incrementorCandidates.Add(new YearIncrementor(5));
            incrementorCandidates.Add(new YearIncrementor(10));
            incrementorCandidates.Add(new YearIncrementor(25));

            // Next we need to eliminate incrementors that would result in a very large number of label
            // recommendations since that would cause the algorithm to be slow and maybe even cause
            // an out of memory exception. 
            for (var i = 0; i < incrementorCandidates.Count; i++)
            {
                var candidate = incrementorCandidates[i];

                if (candidate is TimeSpanIncrementor && (candidate as TimeSpanIncrementor).increment.TotalSeconds * MaxLabelsOnAnyChartAssumption < xRange.TotalSeconds)
                {
                    incrementorCandidates.RemoveAt(i--);
                }
                else if (candidate is HalfMonthIncrementor && 15 * 100 < xRange.TotalDays)
                {
                    incrementorCandidates.RemoveAt(i--);
                }
                else if (candidate is MonthIncrementor && (candidate as MonthIncrementor).increment * 30 * MaxLabelsOnAnyChartAssumption < xRange.TotalDays)
                {
                    incrementorCandidates.RemoveAt(i--);
                }
                else if (candidate is YearIncrementor && (candidate as YearIncrementor).increment * 365 * MaxLabelsOnAnyChartAssumption < xRange.TotalDays)
                {
                    if (incrementorCandidates.Count > 1)
                    {
                        incrementorCandidates.RemoveAt(i--);
                    }
                }
            }

            // Finally we run each incrementor and see how many labels it yeilds. The one that
            // gets closest to our max number of labels, without going over... wins.
            var bestIncrementor = incrementorCandidates.OrderBy((si) =>
            {
                var labels = si.GetDateLabels(minX, maxX);
                if (labels.Count > maxNumberOfLabels)
                {
                    return int.MaxValue;
                }
                else
                {
                    return maxNumberOfLabels - labels.Count;
                }
            }).First();

            return bestIncrementor.GetDateLabels(minX, maxX).Select(d => (double)d.Ticks).ToList();
        }

        /// <summary>
        /// Formats the given value for display
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="value">the value to format</param>
        /// <returns>the formatted value</returns>
        public ConsoleString FormatValue(double min, double max, double value)
        {
            var t = new DateTime((long)value);
            // we have an even day
            if (t.Round(TimeSpan.FromDays(1)) == t)
            {
                return t.ToString("M/d/yyyy").ToConsoleString();
            }
            // we have an even hour that is not on a day boundary
            else if (t.Round(TimeSpan.FromHours(1)) == t)
            {
                return t.ToString("h tt").ToConsoleString();
            }
            // we have an even minute that is not on an hour boundary
            else if (t.Round(TimeSpan.FromMinutes(1)) == t)
            {
                return t.ToString("h:mm").ToConsoleString();
            }
            // we have an even second that is not on a minute boundary
            else if (t.Round(TimeSpan.FromSeconds(1)) == t)
            {
                return t.ToString("h:mm:ss").ToConsoleString();
            }
            // we have an even millisecond that is not on a minute boundary
            else if (t.Round(TimeSpan.FromMilliseconds(1)) == t)
            {
                return t.ToString("ss.fff").ToConsoleString();
            }
            else
            {
                return t.ToString("ss.fffff").ToConsoleString();
            }
        }
    }
    public class TimeSpanFormatter : IAxisFormatter
    {
        const int MaxLabelsOnAnyChartAssumption = 50;

        /// <summary>
        /// Gets the values for ideal increments to be shown on an axis
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="maxNumberOfLabels">the maximun number of labels that can be rendered on the axis</param>
        /// <returns>the ideal increments</returns>
        public List<double> GetOptimizedAxisLabelValues(double min, double max, int maxNumberOfLabels)
        {
            var minX = new TimeSpan((long)min);
            var maxX = new TimeSpan((long)max);
            var xRange = maxX - minX;

            // for really large time spans just use days and fall back to a base 10 incremental scale strategy
            if (xRange.TotalDays > 365 * MaxLabelsOnAnyChartAssumption)
            {
                return new NumberFormatter().GetOptimizedAxisLabelValues(minX.TotalDays, maxX.TotalDays + 1, maxNumberOfLabels)
                    .Select(d => (double)TimeSpan.FromDays(d).Ticks)
                    .ToList();
            }

            var incrementorCandidates = new List<TimeSpanIncrementor>();
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.000025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.00025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.001)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.0025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.005)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.01)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.025)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.05)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.25)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(.5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(15)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromSeconds(30)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(15)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromMinutes(30)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(6)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromHours(12)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(1)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(2)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(5)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(10)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(25)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(50)));
            incrementorCandidates.Add(new TimeSpanIncrementor(TimeSpan.FromDays(100)));

            for (var i = 0; i < incrementorCandidates.Count; i++)
            {
                var candidate = incrementorCandidates[i];

                if (candidate.increment.TotalSeconds * MaxLabelsOnAnyChartAssumption < xRange.TotalSeconds)
                {
                    incrementorCandidates.RemoveAt(i--);
                }
            }

            var bestIncrementor = incrementorCandidates
                .OrderBy((si) =>
                {
                    var labels = si.GetTimeLabels(minX, maxX);
                    if (labels.Count > maxNumberOfLabels)
                    {
                        return int.MaxValue;
                    }
                    else
                    {
                        return maxNumberOfLabels - labels.Count;
                    }
                })
                .First();
            return bestIncrementor.GetTimeLabels(minX, maxX).Select(d => (double)d.Ticks).ToList();
        }

        /// <summary>
        /// Formats the given value for display
        /// </summary>
        /// <param name="min">the lowest value represented on the axis</param>
        /// <param name="max">the highest value represented on the axis</param>
        /// <param name="value">the value to format</param>
        /// <returns>the formatted value</returns>
        public ConsoleString FormatValue(double min, double max, double value)
        {
            var t = new TimeSpan((long)value);
            // we have an even day
            if (t.Round(TimeSpan.FromDays(1)) == t)
            {
                return (t.TotalDays + "d").ToConsoleString();
            }
            // we have an even hour that is not on a day boundary
            else if (t.Round(TimeSpan.FromHours(1)) == t)
            {
                return (t.TotalHours + "h").ToConsoleString();
            }
            // we have an even minute that is not on an hour boundary
            else if (t.Round(TimeSpan.FromMinutes(1)) == t)
            {
                return (t.TotalMinutes + "m").ToConsoleString();
            }
            // we have an even second that is not on a minute boundary
            else if (t.Round(TimeSpan.FromSeconds(1)) == t)
            {
                return (t.TotalSeconds + "s").ToConsoleString();
            }
            else
            {
                return (t.TotalMilliseconds + "ms").ToConsoleString();
            }
        }
    }

    /* 
     * The below classes model time increments that make sense as axis labels. This model is needed
     * since not all meaningful increments can be represented by a time span (e.g. a month or a year).
     */

    internal abstract class TimeIncrementor
    {
        public abstract DateTime Increment(DateTime original);

        public abstract DateTime Floor(DateTime original);

        public List<DateTime> GetDateLabels(DateTime min, DateTime max)
        {
            var current = Floor(min);

            var ret = new List<DateTime>();
            while (current <= max)
            {
                ret.Add(current);
                current = Increment(current);
            }

            return ret;
        }
    }
    internal class TimeSpanIncrementor : TimeIncrementor
    {
        public TimeSpan increment;
        public TimeSpanIncrementor(TimeSpan span) { this.increment = span; }
        public override DateTime Increment(DateTime original) => original + increment;
        public override DateTime Floor(DateTime original) => new DateTime(original.Ticks - (original.Ticks % increment.Ticks));

        public TimeSpan Increment(TimeSpan original) => original + increment;

        public TimeSpan Floor(TimeSpan original) => new TimeSpan(original.Ticks - (original.Ticks % increment.Ticks));

        public List<TimeSpan> GetTimeLabels(TimeSpan min, TimeSpan max)
        {
            var current = Floor(min);

            var ret = new List<TimeSpan>();
            while (current <= max)
            {
                ret.Add(current);
                current = Increment(current);
            }

            return ret;
        }
    }
    internal class HalfMonthIncrementor : TimeIncrementor
    {
        public override DateTime Increment(DateTime original)
        {
            if (original.Day == 15) return new DateTime(original.Year, original.Month, 1).AddMonths(1);
            else if (original.Day == 1) return new DateTime(original.Year, original.Month, 15);
            else throw new ArgumentException("Only supports 1st and 15th");
        }

        public override DateTime Floor(DateTime original)
        {
            if (original.Day >= 15) return new DateTime(original.Year, original.Month, 15);
            else return new DateTime(original.Year, original.Month, 1);
        }
    }
    internal class MonthIncrementor : TimeIncrementor
    {
        public int increment;
        public MonthIncrementor(int increment) { this.increment = increment; }
        public override DateTime Increment(DateTime original) => original.AddMonths(increment);
        public override DateTime Floor(DateTime original) => new DateTime(original.Year, original.Month, 1);
    }
    internal class YearIncrementor : TimeIncrementor
    {
        public int increment;
        public YearIncrementor(int increment) { this.increment = increment; }
        public override DateTime Increment(DateTime original) => original.AddYears(increment);
        public override DateTime Floor(DateTime original) => new DateTime(original.Year - (original.Year % increment), 1, 1);
    }
}
