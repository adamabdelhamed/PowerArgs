using PowerArgs.Cli.Physics;
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
    /// Mode for rendering plots
    /// </summary>
    public enum PlotMode
    {
        /// <summary>
        /// Renders each data point as a single character
        /// </summary>
        Points,
        /// <summary>
        /// Renders bars underneath each data point
        /// </summary>
        Bars,
        /// <summary>
        /// Connects sequential data points with lines
        /// </summary>
        Lines,
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

        /// <summary>
        /// The plot mode, default to points
        /// </summary>
        public PlotMode PlotMode { get; set; } = PlotMode.Points;
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

        /// <summary>
        /// A description of this point
        /// </summary>
        public string Description { get; set; }
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

            public List<BarOrLineControl> BarsOrLines { get; set; } = new List<BarOrLineControl>();
        }

        private class BarOrLineControl : PixelControl
        {
            public BarOrLineControl() { this.CanFocus = false; }
        }

        private const int TitleZIndex = 1;
        private const int LinesAndBarsZIndex = 2;
        private const int DataPointsZIndex = 3;
        private const int FocusedDataPointZIndex = 4;
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

        private double? cachedMinXValueInPlotArea;
        private double MinXValueInPlotArea
        {
            get
            {
                if(cachedMinXValueInPlotArea.HasValue)
                {
                    return cachedMinXValueInPlotArea.Value;
                }

                if (options.XMinOverride.HasValue)
                {
                    return options.XMinOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.X).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.X).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.XAxisRangePadding;
                cachedMinXValueInPlotArea = trueMin - padding / 2.0;
                return cachedMinXValueInPlotArea.Value;
            }
        }

        private double? cachedMinYValueInPlotArea;
        private double MinYValueInPlotArea
        {
            get
            {
                if(cachedMinYValueInPlotArea.HasValue)
                {
                    return cachedMinYValueInPlotArea.Value;
                }

                if (options.YMinOverride.HasValue)
                {
                    return options.YMinOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.Y).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.Y).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.YAxisRangePadding;
                cachedMinYValueInPlotArea =  trueMin - padding / 2.0;
                return cachedMinYValueInPlotArea.Value;
            }
        }

        private double? cachedMaxXValueInPlotArea;
        private double MaxXValueInPlotArea
        {
            get
            {
                if(cachedMaxXValueInPlotArea.HasValue)
                {
                    return cachedMaxXValueInPlotArea.Value;
                }

                if (options.XMaxOverride.HasValue)
                {
                    return options.XMaxOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.X).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.X).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.XAxisRangePadding;
                cachedMaxXValueInPlotArea =  trueMax + padding / 2.0;
                return cachedMaxXValueInPlotArea.Value;
            }
        }

        private double? cachedMaxYValueInPlotArea;
        private double MaxYValueInPlotArea
        {
            get
            {
                if(cachedMaxYValueInPlotArea.HasValue)
                {
                    return cachedMaxYValueInPlotArea.Value;
                }

                if (options.YMaxOverride.HasValue)
                {
                    return options.YMaxOverride.Value;
                }

                var trueMin = GetMinValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MaxValue : s.Points.Select(p => p.Y).Min());
                var trueMax = GetMaxValueAcrossSeries(options.Data, s => s.Points.Count == 0 ? double.MinValue : s.Points.Select(p => p.Y).Max());
                PadZeroRangeIfNeeded(ref trueMin, ref trueMax);
                var trueRange = trueMax - trueMin;
                var padding = trueRange * options.YAxisRangePadding;
                cachedMaxYValueInPlotArea = trueMax + padding / 2.0;
                return cachedMaxYValueInPlotArea.Value;
            }
        }

        private Label chartTitleLabel;
        private Label seriesTitleLabel;

        private List<AxisLabelInfo> cachedXAxisLabels;
        private List<AxisLabelInfo> cachedYAxisLabels;


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

            var defaultSeriesTitle = ConsoleString.Empty;

            if (options.Data.First().AllowInteractivity == false || options.Data.Count == 1)
            {
                defaultSeriesTitle = options.Data.First().Title.ToConsoleString(options.Data.First().PlotCharacter.ForegroundColor, options.Data.First().PlotCharacter.BackgroundColor);
            }

            Ready.SubscribeOnce(() =>
            { 
                ConsoleApp.Current.FocusManager.SubscribeForLifetime(nameof(FocusManager.FocusedControl), () =>
                {
                     if(options.Data.Count > 1 && ConsoleApp.Current.FocusManager.FocusedControl is DataPointControl == false)
                     {
                         seriesTitleLabel.Text = ConsoleString.Empty;
                     }

                 }, this);
            });

            chartTitleLabel = Add(new Label() { ZIndex = TitleZIndex, Text = options.Title }).CenterHorizontally().DockToTop(padding: 2);
            seriesTitleLabel = Add(new Label() { ZIndex = TitleZIndex, Text = defaultSeriesTitle }).CenterHorizontally().DockToTop(padding: 3);

            this.CanFocus = true;
            Lifetime focusLt = null;
            this.Focused.SubscribeForLifetime(() =>
            {
                focusLt = new Lifetime();
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, HandleUpArrow, focusLt);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, HandleDownArrow, focusLt);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.LeftArrow, null, HandleLeftArrow, focusLt);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, HandleRightArrow, focusLt);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Home, null, HandleHomeKey, focusLt);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.End, null, HandleEndKey, focusLt);
            }, this);

            this.Unfocused.SubscribeForLifetime(() =>
            {
                focusLt?.Dispose();
                focusLt = null;
            }, this);
 
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
        public static void Show(IEnumerable<DataPoint> data) => Show(new XYChartOptions() { Data = new List<Series>() { new Series() { Points = data.ToList() } } });

        /// <summary>
        /// Shows the data in a chart via an interactive console app
        /// </summary>
        /// <param name="options">options used to render the chart</param>
        public static void Show(XYChartOptions options) => ConsoleApp.Show(new XYChart(options));

        /// <summary>
        /// Re-evaluates the data and re-renders the chart. You need to call this method if you have
        /// modified the data since the last refresh or since calling the chart's constructor
        /// </summary>
        public void Refresh()
        {
            this.chartTitleLabel.Text = options.Title;
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

            if (MinXValueInPlotArea != double.PositiveInfinity && MaxXValueInPlotArea != double.NegativeInfinity)
            {
                cachedXAxisLabels = cachedXAxisLabels ??
                    options.XAxisFormatter.GetOptimizedAxisLabelValues(MinXValueInPlotArea, MaxXValueInPlotArea, maxNumberOfLabels).Select(d =>
                    {
                        var label = options.XAxisFormatter.FormatValue(MinXValueInPlotArea, MaxXValueInPlotArea, d);

                        if (label.Length > MaxXAxisLabelLength)
                        {
                            label = label.Substring(0, MaxXAxisLabelLength);
                        }
                        return new AxisLabelInfo() { Label = label, Value = d };
                    }).ToList();

                foreach (var labelInfo in cachedXAxisLabels)
                {
                    var x = ConvertXValueToPixel(labelInfo.Value);
                    var y = XAxisYValue + 1;

                    if (x + labelInfo.Label.Length <= Width)
                    {
                        context.Pen = new ConsoleCharacter('^', Foreground, Background);
                        context.DrawPoint(x, y - 1);
                        context.DrawString(labelInfo.Label, x, y);
                    }
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
            if (cachedYAxisLabels != null) return cachedYAxisLabels;
            var maxNumberOfLabels = YAxisHeight / 3;
            maxYAxisLabelLength = 0;
            var labels = new List<AxisLabelInfo>();


            if (MinYValueInPlotArea != double.MaxValue && MaxYValueInPlotArea != double.NegativeInfinity)
            {
                foreach (var labelValue in options.YAxisFormatter.GetOptimizedAxisLabelValues(MinYValueInPlotArea, MaxYValueInPlotArea, maxNumberOfLabels))
                {
                    var label = options.YAxisFormatter.FormatValue(MinYValueInPlotArea, MaxYValueInPlotArea, labelValue);
                    labels.Add(new AxisLabelInfo() { Label = label, Value = labelValue });
                    maxYAxisLabelLength = Math.Max(maxYAxisLabelLength, label.Length);
                }
            }
            cachedYAxisLabels = labels;
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
                context.DrawPoint(maxYAxisLabelLength, y);
            }
        }

        private void AddDataPoints()
        {
            cachedMinXValueInPlotArea = null;
            cachedMinYValueInPlotArea = null;
            cachedMaxXValueInPlotArea = null;
            cachedMaxYValueInPlotArea = null;
            foreach(var control in this.Controls.Where(c => c != chartTitleLabel && c != seriesTitleLabel ).ToArray())
            {
                this.Controls.Remove(control);
            }

            foreach (var series in options.Data)
            {
                for (int i = 0; i < series.Points.Count; i++)
                {
                    var pixel = new DataPointControl() { ZIndex = DataPointsZIndex, CanFocus = series.AllowInteractivity, DataPoint = series.Points[i], Series = series, Value = series.PlotCharacter };
                    pixel.Focused.SubscribeForLifetime(() =>
                    {
                        pixel.ZIndex = FocusedDataPointZIndex;
                        pixel.Value = new ConsoleCharacter(series.PlotCharacter.Value, ConsoleColor.Black, ConsoleColor.Cyan);
                        pixel.BarsOrLines.ForEach(b => b.Value = pixel.Value);
                        pixel.BarsOrLines.ForEach(b => b.ZIndex = pixel.ZIndex);
                        var newTitle = pixel.Series.Title.ToConsoleString(pixel.Series.PlotCharacter.ForegroundColor);

                        var xValue = options.XAxisFormatter.FormatValue(MinXValueInPlotArea, MaxXValueInPlotArea, pixel.DataPoint.X);
                        var yValue = options.YAxisFormatter.FormatValue(MinYValueInPlotArea, MaxYValueInPlotArea, pixel.DataPoint.Y);

                        if (pixel.DataPoint.Description == null)
                        {
                            newTitle += new ConsoleString(" ( " + xValue + ", " + yValue + " )", series.PlotCharacter.ForegroundColor, series.PlotCharacter.BackgroundColor);
                        }
                        else
                        {
                            newTitle+= new ConsoleString($" ( {xValue},{yValue} - {pixel.DataPoint.Description} )", series.PlotCharacter.ForegroundColor, series.PlotCharacter.BackgroundColor);
                        }
                        seriesTitleLabel.Text = newTitle;
                    }, pixel);

                    pixel.Unfocused.SubscribeForLifetime(() =>
                    {
                        pixel.Value = series.PlotCharacter;
                        pixel.BarsOrLines.ForEach(b => b.Value = pixel.Value);
                        pixel.BarsOrLines.ForEach(b => b.ZIndex = LinesAndBarsZIndex);
                        pixel.ZIndex = DataPointsZIndex;
                    }, pixel);
                    this.Controls.Add(pixel);
                }
            }
        }

        private void PositionDataPoints()
        {
            cachedXAxisLabels = null;
            cachedYAxisLabels = null;
            DetermineYAxisLabels(); // ensures the y axis is offset properly due to variable label widths

            this.Controls.WhereAs<BarOrLineControl>().ToList().ForEach(c => Controls.Remove(c));
            var dataPointControlsGroups = Controls.Where(c => c is DataPointControl).Select(c => c as DataPointControl).GroupBy(c => c.Series).ToList();

            foreach (var seriesOfCOntrols in dataPointControlsGroups)
            {
                var dataPointControls = seriesOfCOntrols.ToList();
                for (var i = 0; i < dataPointControls.Count; i++)
                {
                    var control = dataPointControls[i];
                    control.BarsOrLines.Clear();
                    var newX = ConvertXValueToPixel(control.DataPoint.X);
                    var newY = ConvertYValueToPixel(control.DataPoint.Y);
                    control.X = newX;
                    control.Y = newY;
                    if (newX >= 0 && newX < Width && newY >= 0 && newY < Height)
                    {
                        control.IsVisible = true;
                    }
                    else
                    {
                        control.IsVisible = false;
                    }

                    if (control.Series.PlotMode == PlotMode.Bars)
                    {
                        for (var y = control.Y + 1; y < YAxisBottom; y++)
                        {
                            var barPixel = new BarOrLineControl()
                            {
                                X = control.X,
                                Y = y,
                                ZIndex = control.HasFocus ? control.ZIndex : LinesAndBarsZIndex,
                                Value = control.Value
                            };
                            Controls.Add(barPixel);
                            control.BarsOrLines.Add(barPixel);
                        }
                    }
                    else if (control.Series.PlotMode == PlotMode.Lines && i < dataPointControls.Count - 1)
                    {
                        var nextControl = dataPointControls[i + 1];
                        var newX2 = ConvertXValueToPixel(nextControl.DataPoint.X);
                        var newY2 = ConvertYValueToPixel(nextControl.DataPoint.Y);
                        if (newX2 >= 0 && newX2 < Width && newY2 >= 0 && newY2 < Height)
                        {
                            nextControl.X = newX2;
                            nextControl.Y = newY2;

                            var len = ConsoleBitmap.DefineLineBuffered(control.X, control.Y, nextControl.X, nextControl.Y);
                            for(var j = 0; j < len; j++)
                            {
                                var point = ConsoleBitmap.LineBuffer[j];
                                var line = new BarOrLineControl()
                                {
                                    X = point.X,
                                    Y = point.Y,
                                    ZIndex = LinesAndBarsZIndex,
                                    Value = new ConsoleCharacter('-', control.Series.PlotCharacter.ForegroundColor, control.Series.PlotCharacter.BackgroundColor)
                                };
                                control.BarsOrLines.Add(line);
                                Controls.Add(line);
                            }
                        }
                    }
                }
            }
       }

        private void HandleHomeKey()
        {
            Controls
                .WhereAs<DataPointControl>()
                .Where(p => p.CanFocus)
                .OrderBy(p => p.X)
                .ThenByDescending(p => p.Y)
                .FirstOrDefault()
                ?.TryFocus();
        }

        private void HandleEndKey()
        {
            Controls
                .WhereAs<DataPointControl>()
                .Where(p => p.CanFocus)
                .OrderByDescending(p => p.X)
                .ThenBy(p => p.Y)
                .FirstOrDefault()
                ?.TryFocus();
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
                return ConsoleMath.Round(xConverted);
            }
        }

        // even though this is not used today it might be if I
        // ever wanted to have something like focusable axis labels
        // that show the value at the tick or any arbitrary pixel in
        // the plot area. This way I don't have to revisit that math later.
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
                return ConsoleMath.Round((float)yConverted);
            }

        }

        // even though this is not used today it might be if I
        // ever wanted to have something like focusable axis labels
        // that show the value at the tick or any arbitrary pixel in
        // the plot area. This way I don't have to revisit that math later.
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
            double ret = double.MinValue;

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

        /// <summary>
        /// In the case where all the values on a given axis are the same then
        /// we need to choose a reasonable axis range to show the perspective of the
        /// data. This method will do that by updating the given min and max values
        /// if they are the same.
        /// </summary>
        /// <param name="min">the min value on an axis</param>
        /// <param name="max">the max value on an axis</param>
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

        /// <summary>
        /// Finds the distance between the given points using the distance
        /// formula we learn in Algebra class.
        /// </summary>
        /// <param name="a">the first point</param>
        /// <param name="b">the second point</param>
        /// <returns>the distance between the given points</returns>
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
            if (value == ConsoleMath.Round(value))
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

    /// <summary>
    /// An axis formatter capable of rendering labels for TimeSpan values
    /// </summary>
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
            var minTime = new TimeSpan((long)min);
            var maxTime = new TimeSpan((long)max);
            var range = maxTime - minTime;

            // we have an even day
            if (t.Round(TimeSpan.FromDays(1)) == t)
            {
                if (t.TotalDays == 0 && range < TimeSpan.FromSeconds(1))
                {
                    return "0ms".ToConsoleString();
                }
                else if (t.TotalDays == 0 && range < TimeSpan.FromMinutes(1))
                {
                    return "0s".ToConsoleString();
                }
                else if(t.TotalDays == 0 && range < TimeSpan.FromHours(1))
                {
                    return "0m".ToConsoleString();
                }
                else if (t.TotalDays == 0 && range < TimeSpan.FromHours(24))
                {
                    return "0h".ToConsoleString();
                }
                else
                {
                    return (t.TotalDays + "d").ToConsoleString();
                }
            }
            // we have an even hour that is not on a day boundary
            else if (t.Round(TimeSpan.FromHours(1)) == t)
            {
                if (t.TotalHours == 0 && range < TimeSpan.FromSeconds(1))
                {
                    return "0ms".ToConsoleString();
                }
                else if (t.TotalHours == 0 && range < TimeSpan.FromMinutes(1))
                {
                    return "0s".ToConsoleString();
                }
                else if (t.TotalHours == 0 && range < TimeSpan.FromHours(1))
                {
                    return "0m".ToConsoleString();
                }
                else
                {
                    return (t.TotalHours + "h").ToConsoleString();
                }
            }
            // we have an even minute that is not on an hour boundary
            else if (t.Round(TimeSpan.FromMinutes(1)) == t)
            {
                if (t.TotalMinutes == 0 && range < TimeSpan.FromSeconds(1))
                {
                    return "0ms".ToConsoleString();
                }
                else if (t.TotalMinutes == 0 && range < TimeSpan.FromMinutes(1))
                {
                    return "0s".ToConsoleString();
                }
                else
                {
                    return (t.TotalMinutes + "m").ToConsoleString();
                }
            }
            // we have an even second that is not on a minute boundary
            else if (t.Round(TimeSpan.FromSeconds(1)) == t)
            {
                if (t.TotalSeconds == 0 && range < TimeSpan.FromSeconds(1))
                {
                    return "0ms".ToConsoleString();
                }
                else
                {
                    return (t.TotalSeconds + "s").ToConsoleString();
                }
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
