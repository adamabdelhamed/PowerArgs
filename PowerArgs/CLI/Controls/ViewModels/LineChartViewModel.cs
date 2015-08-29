using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A view model to be used in conjunction with the LineChart control. 
    /// </summary>
    public class LineChartViewModel : ViewModelBase
    {
        public event Action FocusedSeriesChanged;
        public event Action FocusedDataPointChanged;

        /// <summary>
        /// If explicitly set then the minimum value of the Y axis will be forced to the value.  Otherwise, that value will be determined by the data.
        /// </summary>
        public double? YMinimumOverride { get { return Get<double?>(); } set { Set<double?>(value); } }

        /// <summary>
        /// If explicitly set then the maximum value of the Y axis will be forced to the value.  Otherwise, that value will be determined by the data.
        /// </summary>
        public double? YMaximumOverride { get { return Get<double?>(); } set { Set<double?>(value); } }
        public double? XMinimumOverride { get { return Get<double?>(); } set { Set<double?>(value); } }
        public double? XMaximumOverride { get { return Get<double?>(); } set { Set<double?>(value); } }        

        public ObservableCollection<DataSeries> DataSeriesCollection { get; private set; }

        public DataSeries FocusedDataSeries
        {
            get
            {
                if (DataSeriesCollection.Count == 0)
                {
                    return null;
                }
                else if(FocusedDataSeriesIndex >= 0 && FocusedDataSeriesIndex < DataSeriesCollection.Count)
                {
                    return DataSeriesCollection[FocusedDataSeriesIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        public int FocusedDataSeriesIndex { get { return Get<int>(); } set { Set<int>(value); } }
        public int FocusedDataPointIndex { get { return Get<int>(); } set { Set<int>(value); } }

        public LineChartViewModel()
        {
            DataSeriesCollection = new ObservableCollection<DataSeries>();
            DataSeriesCollection.Added += SeriesAdded;
            DataSeriesCollection.Removed += SeriesRemoved;
            this.PropertyChanged += LineChartViewModel_PropertyChanged;
        }

        void LineChartViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "FocusedDataSeriesIndex" && FocusedSeriesChanged != null)
            {
                FocusedSeriesChanged();
            }
            else if (e.PropertyName == "FocusedDataPointIndex" && FocusedDataPointChanged != null)
            {
                FocusedDataPointChanged();
            }
        }

        private void SeriesRemoved(DataSeries series)
        {
            this.FirePropertyChanged("DataSeriesCollection");
            series.DataPoints.Added -= DataPointAdded;
            ResetFocusedSeries();
        }

        private void SeriesAdded(DataSeries series)
        {
            this.FirePropertyChanged("DataSeriesCollection");
            series.DataPoints.Added += DataPointRemoved;
            ResetFocusedSeries();
        }

        private void ResetFocusedSeries()
        {
            FocusedDataSeriesIndex = DataSeriesCollection.Count == 0 ? -1 : 0;
        }

        private void DataPointAdded(DataPoint obj)
        {
            if(FocusedDataSeriesIndex < 0)
            {
                FocusedDataSeriesIndex = 0;
            }
            
            if(FocusedDataPointIndex < 0)
            {
                FocusedDataPointIndex = 0;
            }

            this.FirePropertyChanged("DataSeriesCollection");
        }

        private void DataPointRemoved(DataPoint obj)
        {
            if(FocusedDataSeriesIndex >= 0 && FocusedDataPointIndex >= 0 && FocusedDataPointIndex >= FocusedDataSeries.DataPoints.Count)
            {
                if(FocusedDataSeries.DataPoints.Count == 0)
                {
                    FocusedDataPointIndex = -1;
                }
                else
                {
                    FocusedDataPointIndex--;
                }
            }

            this.FirePropertyChanged("DataSeriesCollection");
        }

        public void IncrementFocusedDataPointIndex(int amount)
        {
            if (FocusedDataSeries == null) return;
            FocusedDataPointIndex+=amount;
            if(FocusedDataPointIndex >= FocusedDataSeries.DataPoints.Count)
            {
                FocusedDataPointIndex = 0;
            }
            else if(FocusedDataPointIndex < 0)
            {
                FocusedDataPointIndex = FocusedDataSeries.DataPoints.Count-1;
            }
        }

        public void IncrementFocusedDataSeriesIndex(int amount)
        {
            if (DataSeriesCollection.Count < 2) return;

            FocusedDataSeriesIndex += amount;
            if (FocusedDataSeriesIndex >= DataSeriesCollection.Count)
            {
                FocusedDataSeriesIndex = 0;
            }
            else if (FocusedDataSeriesIndex < 0)
            {
                FocusedDataSeriesIndex = DataSeriesCollection.Count - 1;
            }
        }

        public double MaxXValue
        {
            get
            {
                if (XMaximumOverride.HasValue)
                {
                    return XMaximumOverride.Value;
                }
                else
                {
                    var max = DataSeries.Max(DataSeriesCollection, s => s.DataPoints.Count == 0 ? 0 : s.DataPoints.Select(p => p.X).Max());
                    return max;
                }
            }
        }

        public double MaxYValue
        {
            get
            {
                if (YMaximumOverride.HasValue)
                {
                    return YMaximumOverride.Value;
                }
                else
                {
                    var max = DataSeries.Max(DataSeriesCollection, s => s.DataPoints.Count == 0 ? 0 : s.DataPoints.Select(p => p.Y).Max());
                    return max;
                }
            }
        }

        public double MinXValue
        {
            get
            {
                if (XMinimumOverride.HasValue)
                {
                    return XMinimumOverride.Value;
                }
                else
                {
                    var min = DataSeries.Min(DataSeriesCollection, s => s.DataPoints.Count == 0 ? 0 : s.DataPoints.Select(p => p.X).Min());
                    return min;
                }
            }
        }

        public double MinYValue
        {
            get
            {
                if (YMinimumOverride.HasValue)
                {
                    return YMinimumOverride.Value;
                }
                else
                {
                    var min = DataSeries.Min(DataSeriesCollection, s => s.DataPoints.Count == 0 ? 0 : s.DataPoints.Select(p => p.Y).Min());
                    return min;
                }
            }
        }
    }

    public enum ThresholdType
    {
        Maximum, 
        Minimum
    }

    public class Threshold : ViewModelBase
    {
        public ThresholdType Type { get; set; }
        public double Value { get { return Get<double>(); } set { Set<double>(value); } }
        public string Title { get { return Get<string>(); } set { Set<string>(value); } }

        public ConsoleColor PlotColor { get; set; }
        public ConsoleColor ActiveColor { get; set; }

        public Threshold()
        {
            ActiveColor = ConsoleColor.Red;
            PlotColor = ConsoleColor.DarkGray;
            Title = "Threshold";
        }

        public bool IsActive(double value)
        {
            if(Type == ThresholdType.Maximum && value >= Value)
            {
                return true;
            }
            else if(Type == ThresholdType.Minimum && value <= Value)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class DataSeries : ViewModelBase
    {
        public bool ShowAreaUnderEachDataPoint { get { return Get<bool>(); } set { Set<bool>(value); } }
        public Threshold Threshold { get; set; }
        public string Title { get { return Get<string>(); } set { Set<string>(value); } }

        public char PlotCharacter { get; set; }

        public ConsoleColor PlotColor { get; set; }

        public ObservableCollection<DataPoint> DataPoints { get; private set; }

        public double MinXValue
        {
            get
            {
                return DataPoints.Count == 0 ? 0 : DataPoints.Select(p => p.X).Min();
            }
        }

        public double MaxXValue
        {
            get
            {
                return DataPoints.Count == 0 ? 0 : DataPoints.Select(p => p.X).Max();
            }
        }

        public double MinYValue
        {
            get
            {
                return DataPoints.Count == 0 ? 0 : DataPoints.Select(p => p.Y).Min();
            }
        }

        public double MaxYValue
        {
            get
            {
                return DataPoints.Count == 0 ? 0 : DataPoints.Select(p => p.Y).Max();
            }
        }

        public static double Max(IEnumerable<DataSeries> seriesCollection, Func<DataSeries,double> maxFunc)
        {
            double ret = 0;

            foreach(var series in seriesCollection)
            {
                ret = Math.Max(ret, maxFunc(series));
            }

            return ret;
        }

        public static double Min(IEnumerable<DataSeries> seriesCollection, Func<DataSeries, double> minFunc)
        {
            double ret = 0;

            foreach (var series in seriesCollection)
            {
                ret = Math.Min(ret, minFunc(series));
            }

            return ret;
        }

        public DataSeries()
        {
            DataPoints = new ObservableCollection<DataPoint>();
            PlotCharacter = 'x';
            PlotColor = ConsoleColor.White;
        }


    }

    public class DataPoint : ViewModelBase
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
