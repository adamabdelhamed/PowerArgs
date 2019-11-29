using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public class TimeDebuggingData
    {
        public Dictionary<TimeSpan, List<string>> Data { get; private set; } = new Dictionary<TimeSpan, List<string>>();
        
        public void Track(string reason)
        {
            if (Data.TryGetValue(Time.CurrentTime.Now, out List<string> reasons) == false)
            {
                reasons = new List<string>();
                Data.Add(Time.CurrentTime.Now, reasons);
            }
            reasons.Add(reason);
        }
    }

    public class TimeDebuggerPanel : ConsolePanel
    {
        public TimeDebuggerSettings Settings { get; private set; } = new TimeDebuggerSettings();

        private TimeDebuggingData data;
        private GridLayout grid;
        private XYChart chart;
        private XYChartOptions chartOptions;
        private TimeSpan maxTime;
        private bool settingsHasChanged;
        public TimeDebuggerPanel(TimeDebuggingData data)
        {
            this.data = data;
            InitGridLayout();
            InitLeftPane();
            grid.RefreshLayout();
            SetDefaultMinReasonsPerTick();
            InitChart();
        }

        private void InitLeftPane()
        {
            var stack = grid.Add(new StackPanel() { AutoSize = false, Orientation = Orientation.Vertical }, 0, 0);
            var formOptions = FormOptions.FromObject(Settings);
            formOptions.LabelColumnPercentage = .7f;
            var settingsForm = stack.Add(new Form(formOptions) { Height = 10 }).FillHorizontally();

            Settings.SuppressEqualChanges = true;
            Settings.SubscribeForLifetime(AnyProperty, () =>
            {
                settingsHasChanged = true;
            }, this);

            ConsoleApp.Current.SetTimeout(() =>
            {
                foreach (var control in settingsForm.Descendents)
                {
                    control.Unfocused.SubscribeForLifetime(()=>
                    {
                        if(settingsHasChanged)
                        {
                            RefreshChart();
                            settingsHasChanged = false;
                        }

                    }, this);
                }
            },TimeSpan.FromSeconds(1));
         
        }

        private void InitChart()
        {
            maxTime = data.Data.Keys.Max();
            chart = grid.Add(new XYChart(chartOptions = new XYChartOptions()
            {
                Title = "Events per tick".ToWhite(),
                XAxisFormatter = new TimeSpanFormatter(),
                YMinOverride = 0,
                XMinOverride = 0,
                XMaxOverride = maxTime.Ticks,
                Data = new List<Series>()
                {
                    new Series()
                    {
                        Points = GetPoints(),
                        AllowInteractivity = true,
                    },
                    new Series()
                    {
                        Points = GetCloggedPoints(),
                        AllowInteractivity = true,
                        PlotCharacter = new ConsoleCharacter('#', ConsoleColor.Red)
                    }
                }
            }), 1, 0);
            Settings.PointsVisualized = chartOptions.Data[0].Points.Count;
        }

        private void RefreshChart()
        {
            if (chartOptions == null) return;
            chartOptions.Data[0].Points = GetPoints();
            chart.Refresh();
            Settings.PointsVisualized = chartOptions.Data[0].Points.Count;
        }

        private void InitGridLayout()
        {
            this.grid = Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = new List<GridColumnDefinition>()
                {
                    new GridColumnDefinition(){ Width = .25, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .75, Type = GridValueType.Percentage },
                },
                Rows = new List<GridRowDefinition>()
                {
                    new GridRowDefinition(){ Height = 1, Type = GridValueType.Percentage }
                }
            })).Fill();
            grid.RefreshLayout();
        }

        private void SetDefaultMinReasonsPerTick()
        {
            var counts = data.Data.Select(d => d.Value.Count()).OrderBy(c => c).ToList();
            var cutoff = counts[(int)(counts.Count *.95f)];
            Settings.MinReasonsPerTick = cutoff + 1;
        }

        private Dictionary<TimeSpan,List<string>> FilterData()
        {
            var smallerData = new Dictionary<TimeSpan, List<string>>();
            foreach (var pair in data.Data.Where(d => d.Value.Count >= Settings.MinReasonsPerTick))
            {
                var innerList = new List<string>();
                foreach(var str in pair.Value)
                {
                    if (str != "Clogged")
                    {
                        if (string.IsNullOrWhiteSpace(Settings.SearchFilter) || str.IndexOf(Settings.SearchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            innerList.Add(str);
                        }
                    }
                }
                smallerData.Add(pair.Key, innerList);
            }
            return smallerData;
        }

        private List<DataPoint> GetPoints()
        {
            var ret = new List<DataPoint>();
            foreach(var ev in FilterData())
            {
                for(var i = 0; i < ev.Value.Count; i++)
                {
                    ret.Add(new DataPoint() { X = ev.Key.Ticks, Y = i, Description = ev.Value[i] });
                }
            }
            return ret;
        }

        private List<DataPoint> GetCloggedPoints()
        {
            var ret = new List<DataPoint>();
            foreach (var ev in data.Data)
            {
                if(ev.Value.Where(v => v == "Clogged").Any())
                { 
                    ret.Add(new DataPoint() { X = ev.Key.Ticks, Y = 0, Description = "Clogged" });
                }
            }
            return ret;
        }
    }

    public class TimeDebuggerSettings : ObservableObject
    {
        public int MinReasonsPerTick { get => Get<int>(); set => Set(value); } 
        public string SearchFilter { get => Get<string>(); set => Set(value); }
        [FormReadOnly]
        public int PointsVisualized { get => Get<int>(); set => Set(value); }
    }
}
