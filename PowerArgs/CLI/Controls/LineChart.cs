using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public static class ChartLabelFormatters
    {
        public static Func<double, ConsoleString> SecondGranularityCompactTimestamp = (d) =>
        {
            var dateTime = new DateTime((long)d);

            if (dateTime.Second <= 5)
            {
                return new ConsoleString(dateTime.ToString("h:mmt"));
            }
            else if (dateTime.Second >= 55)
            {
                return new ConsoleString(dateTime.AddMinutes(1).ToString("h:mmt"));
            }
            else
            {
                return new ConsoleString(dateTime.ToString(":ss"));
            }
        };

        public static Func<double, ConsoleString> SecondGranularityTimestamp = (d) =>
        {
            var dateTime = new DateTime((long)d);
            return new ConsoleString(dateTime.ToString("h:mm:ss tt"));
        };
    }

    public class LineChart : ConsoleControl
    {
        public int YAxisLeftOffset { get; set; }
        const int XAxisBottomOffset = 2;
        public LineChartViewModel ViewModel { get; private set; }

        const char YAxisChar = '|';
        const char XAxisChar = '_';

        public Func<double, ConsoleString> YAxisValueCompactFormatter { get; set; }
        public Func<double, ConsoleString> XAxisValueCompactFormatter { get; set; }

        public Func<double, ConsoleString> YAxisValueFormatter { get; set; }
        public Func<double, ConsoleString> XAxisValueFormatter { get; set; }

        private int XAxisYValue
        {
            get
            {
                return this.Height - XAxisBottomOffset;
            }
        }

        private int YAxisTop
        {
            get
            {
                return 0;
            }
        }

        private int XAxisRight
        {
            get
            {
                return Width - 1;
            }
        }

        private int XAxisLeft
        {
            get
            {
                return YAxisLeftOffset;
            }
        }

        private int YAxisBottom
        {
            get
            {
                return XAxisYValue;
            }
        }

        private int XAxisWidth
        {
            get
            {
                return XAxisRight - XAxisLeft;
            }
        }

        private int YAxisHeight
        {
            get
            {
                return YAxisBottom - YAxisTop;
            }
        }

        public int MaxYAxisLabelLength
        {
            get
            {
                return YAxisLeftOffset - 2;
            }
        }

        public int MaxXAxisLabelLength
        {
            get
            {
                return 6;
            }
        }

        public int MaxTitleLength
        {
            get
            {
                return XAxisWidth - 2;
            }
        }
        
        public LineChart()
        {
            ViewModel = new LineChartViewModel();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            YAxisLeftOffset = 14;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(Application != null && Application.IsRunning)
            {
                Application.Paint();
            }
        }

        public override void OnAdd(ConsoleControl parent)
        {
            base.OnAdd(parent);
            this.Foreground = parent.Foreground;
            this.Background = parent.Background;

            XAxisValueCompactFormatter = XAxisValueCompactFormatter ?? ((d) => {return new ConsoleString(string.Format("{0:0,0.0}",d),Foreground.ForegroundColor, Foreground.BackgroundColor);});
            YAxisValueCompactFormatter = YAxisValueCompactFormatter ?? ((d) => { return new ConsoleString("" + string.Format("{0:0,0.0}",d), Foreground.ForegroundColor, Foreground.BackgroundColor); });

            XAxisValueFormatter = XAxisValueFormatter ?? ((d) => { return new ConsoleString(string.Format("{0:0,0.0}", d), Foreground.ForegroundColor, Foreground.BackgroundColor); });
            YAxisValueFormatter = YAxisValueFormatter ?? ((d) => { return new ConsoleString("" + string.Format("{0:0,0.0}", d), Foreground.ForegroundColor, Foreground.BackgroundColor); });
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.LeftArrow)
            {
                ViewModel.IncrementFocusedDataPointIndex(-1);
            }
            else if(info.Key == ConsoleKey.RightArrow)
            {
                ViewModel.IncrementFocusedDataPointIndex(1);
            }
            else if(info.Key == ConsoleKey.UpArrow)
            {
                ViewModel.IncrementFocusedDataSeriesIndex(-1);
            }
            else if (info.Key == ConsoleKey.DownArrow)
            {
                ViewModel.IncrementFocusedDataSeriesIndex(1);
            }
            else if(info.Key == ConsoleKey.Home)
            {
                ViewModel.FocusedDataPointIndex = 0;
            }
            else if(info.Key == ConsoleKey.End)
            {
                ViewModel.FocusedDataPointIndex = ViewModel.FocusedDataSeries.DataPoints.Count - 1;
            }
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = Foreground;
            RenderTitle(context);
            RenderXAxis(context);
            RenderYAxis(context);

            RenderThresholds(context);
            RenderDataPoints(context);
        }

        private void RenderThresholds(ConsoleBitmap context)
        {
            foreach (var series in ViewModel.DataSeriesCollection.OrderBy(s => s == ViewModel.FocusedDataSeries ? 1 : 0))
            {
                RenderThreshold(context, series);
            }
        }

        private void RenderYAxis(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(YAxisChar, Foreground.ForegroundColor, Foreground.BackgroundColor);
            context.DrawLine(YAxisLeftOffset, YAxisTop, YAxisLeftOffset, XAxisYValue+1);
            RenderYAxisLabels(context);
        }

        private void RenderXAxisLabels(ConsoleBitmap context)
        {
            ConsoleString lastLabel = null;
            int y = XAxisYValue + 1;
            for(int x = XAxisLeft; x < XAxisRight; x+=MaxXAxisLabelLength+1)
            {
                var xConverted = ConvertXPixelToValue(x);
                var label = XAxisValueCompactFormatter(xConverted);

                if (label.Length > MaxXAxisLabelLength)
                {
                    label = label.Substring(0, MaxXAxisLabelLength - 1).AppendUsingCurrentFormat("_");
                }

                if (label != lastLabel)
                {
                    context.DrawString(label, x, y);
                    lastLabel = label;
                }
            }
        }

        private void RenderYAxisLabels(ConsoleBitmap context)
        {
            ConsoleString lastLabel = null;
            for(int y = YAxisTop; y <= YAxisBottom; y+=2)
            {
                double yConverted = ConvertYPixelToValue(y);
                var label = YAxisValueCompactFormatter(yConverted);
                if(label.Length > MaxYAxisLabelLength)
                {
                    label = label.Substring(0, MaxYAxisLabelLength-1).AppendUsingCurrentFormat("_");
                }

                label = label.ToDifferentBackground(Foreground.BackgroundColor);

                var labelLeft = YAxisLeftOffset - 1 - label.Length;

                if (label != lastLabel)
                {
                    context.DrawString(label, labelLeft, y);
                    lastLabel = label;
                }
            }
        }

        private void RenderTitle(ConsoleBitmap context)
        {
            int yOffset = 0;
            foreach (var series in ViewModel.DataSeriesCollection)
            {
                var title = new ConsoleString(series.Title, series.PlotColor, Foreground.BackgroundColor);

                if (HasFocus && ViewModel.FocusedDataPointIndex >= 0 && ViewModel.FocusedDataPointIndex < series.DataPoints.Count && ViewModel.FocusedDataSeries == series)
                {
                    var xValue = XAxisValueFormatter(series.DataPoints[ViewModel.FocusedDataPointIndex].X);
                    var yValue = YAxisValueFormatter(series.DataPoints[ViewModel.FocusedDataPointIndex].Y);
                    title += new ConsoleString(" ( " + xValue + ", " + yValue + " )", FocusForeground.ForegroundColor);
                }

                if (title.Length > MaxTitleLength)
                {
                    title = title.Substring(0, MaxTitleLength)+("_");
                }

                var titleLeft = XAxisLeft + ((XAxisWidth / 2) - (title.Length / 2));
                context.DrawString(title, titleLeft, YAxisTop + 1+yOffset);
                yOffset++;
            }
        }

        private void RenderXAxis(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(XAxisChar, Foreground.ForegroundColor, Foreground.BackgroundColor);
            context.DrawLine(YAxisLeftOffset, XAxisYValue, XAxisRight, XAxisYValue);
            RenderXAxisLabels(context);
        }

        private void RenderDataPoints(ConsoleBitmap context)
        {
            foreach (var series in ViewModel.DataSeriesCollection.OrderBy(s => s == ViewModel.FocusedDataSeries ? 1 : 0))
            {
                if (series == ViewModel.FocusedDataSeries && ViewModel.FocusedDataPointIndex >= series.DataPoints.Count)
                {
                    ViewModel.FocusedDataPointIndex = 0;
                }

                for (int i = 0; i < series.DataPoints.Count; i++)
                {
                    var dataPoint = series.DataPoints[i];
                    RenderDataPoint(context, series, dataPoint, i == ViewModel.FocusedDataPointIndex && series == ViewModel.FocusedDataSeries && HasFocus);
                }
            }
        }

        private void RenderThreshold(ConsoleBitmap context, DataSeries series)
        {
            if (series.Threshold == null) return;

            var yPixel = ConvertYValueToPixel(series.Threshold.Value);

            context.Pen = new ConsoleCharacter('-', series.Threshold.PlotColor, Foreground.BackgroundColor);
            context.DrawLine(XAxisLeft, yPixel, XAxisRight, yPixel);

            var title = series.Threshold.Title;

            if(title.Length > XAxisWidth-2)
            {
                title = series.Threshold.Title.Substring(0, XAxisWidth - 3) + "_";
            }

            context.DrawString(new ConsoleString(title, series.Threshold.PlotColor), XAxisLeft + 1, yPixel);
        }

        private void RenderDataPoint(ConsoleBitmap context, DataSeries series, DataPoint p, bool focused)
        {
            var x = ConvertXValueToPixel(p.X);
            var y = ConvertYValueToPixel(p.Y);

            if(focused)
            {
                context.Pen = new ConsoleCharacter(series.PlotCharacter, FocusForeground.ForegroundColor, FocusForeground.BackgroundColor);
            }
            else if(series.Threshold == null || series.Threshold.IsActive(p.Y) == false)
            {
                context.Pen = new ConsoleCharacter(series.PlotCharacter, series.PlotColor, Foreground.BackgroundColor);
            }
            else
            {
                context.Pen = new ConsoleCharacter(series.PlotCharacter, series.Threshold.ActiveColor, Foreground.BackgroundColor);
            }

            context.DrawPoint(x, y);

            while (series.ShowAreaUnderEachDataPoint && ++y <= YAxisBottom)
            {
                context.DrawPoint(x, y);
            }
        }

        private int ConvertXValueToPixel(double x)
        {
            double xRange = ViewModel.MaxXValue - ViewModel.MinXValue;
            double delta = x - ViewModel.MinXValue;
            double percentage = delta / xRange;

            if(percentage < 0 || percentage > 100)
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
                double dataRange = ViewModel.MaxXValue - ViewModel.MinXValue;
                double xConverted = ViewModel.MinXValue + (dataRange * percentage);
                return xConverted;
            }
        }

        private int ConvertYValueToPixel(double y)
        {
            double yRange = ViewModel.MaxYValue - ViewModel.MinYValue;
            double delta = y - ViewModel.MinYValue;
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
                double dataRange = ViewModel.MaxYValue - ViewModel.MinYValue;
                double yConverted = ViewModel.MinYValue + (dataRange * percentage);
                return yConverted;
            }
        }
    }
}
