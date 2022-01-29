using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
namespace PowerArgs.Samples
{

    public class ResourceMonitor : ConsoleApp
    {
        public ResourceMonitor() => InvokeNextCycle(InitAsync);

        private GridLayout layout;
        private ListGrid<IResource> listView;
        private XYChart chart;

        private XYChartOptions chartOptions;
        private ListGridOptions<IResource> listOptions;
        
        private static readonly List<IResource> resources = new List<IResource>()
        {
            new CPUResource(),
            new RAMResource(),
        };

        private List<List<DataPoint>> data = resources.Select(r => new List<DataPoint>()).ToList();

        private async void InitAsync()
        {
            InitLayout();
            InitListView();
            InitChart();
            InitSelectionHandlers();
            InitMinSizeEnforcer();


            var start = DateTime.Now;
            while (true)
            {
                var now = DateTime.Now;
                chartOptions.XMinOverride = now.AddSeconds(-30).Ticks;
                chartOptions.XMaxOverride = now.Ticks;
                for (var i = 0; i < resources.Count; i++)
                {
                    ScopeDataToLast30Seconds(now, i);

                    var sample = resources[i].GetSample();
                    data[i].Add(new DataPoint() { X = now.Ticks, Y = sample });
                }
                listView.Refresh();
                chart.Refresh();
                await Task.Delay(50);
            }
        }

        private void InitLayout()
        {
            layout = LayoutRoot.Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = new List<GridColumnDefinition>()
                {
                    new GridColumnDefinition(){ Type = GridValueType.Pixels, Width = 60 },
                    new GridColumnDefinition(){ Type = GridValueType.RemainderValue, Width = 1 }
                },
                Rows = new List<GridRowDefinition>()
                {
                    new GridRowDefinition(){ Type =  GridValueType.Percentage, Height = 1 }
                }
            })).Fill();
            layout.RefreshLayout();
        }

        private void InitListView()
        {
            listOptions = new ListGridOptions<IResource>()
            {
                Columns = new List<ListGridColumnDefinition<IResource>>()
                {
                    new ListGridColumnDefinition<IResource>()
                    {
                         Header = "Resource".ToYellow(),
                         Formatter = r => new Label(){ Text =  r.DisplayName.ToConsoleString() },
                         Type = GridValueType.RemainderValue,
                         Width = 2,
                    },
                    new ListGridColumnDefinition<IResource>()
                    {
                         Header = "Current Value".ToYellow(),
                         Formatter = r => new Label(){ Text =  r.GetFormattedSample() },
                         Type = GridValueType.RemainderValue,
                         Width = 1,
                    }
                },
                DataSource = new SyncList<IResource>(resources),
                ShowColumnHeaders = true,
                ShowPager = false,
            };
            listView = layout.Add(new ListGrid<IResource>(listOptions), 0, 0);
        }

        private void InitChart()
        {
            chartOptions = new XYChartOptions()
            {
                Title = "Resource Monitor".ToYellow(),
                YMinOverride = 0,
                YAxisRangePadding = .7f,
                XAxisFormatter = new DateTimeFormatter(),
                YAxisFormatter = new NumberFormatter(),
                Data = new List<Series>()
                {
                    new Series()
                    {
                        Title = resources[listView.SelectedRowIndex].DisplayName,
                        PlotCharacter = new ConsoleCharacter('O', ConsoleColor.Cyan),
                        Points = new List<DataPoint>(),
                    }
                }

            };
            chart = layout.Add(new XYChart(chartOptions), 1, 0);
        }

        private void InitSelectionHandlers()
        {
            var gridSelectionHandler = new Action(() =>
            {
                chartOptions.YMaxOverride = resources[listView.SelectedRowIndex].MaxValue;
                chartOptions.Data[0].Title = resources[listView.SelectedRowIndex].DisplayName;
                chartOptions.Data[0].Points = data[listView.SelectedRowIndex];
                chart.Refresh();

            });
            gridSelectionHandler();
            listView.SelectionChanged.SubscribeForLifetime(gridSelectionHandler, this);
        }

        private void InitMinSizeEnforcer()
        {
            LayoutRoot.Add(new MinimumSizeEnforcerPanel(new MinimumSizeEnforcerPanelOptions()
            {
                MinHeight = 15,
                MinWidth = 120,
                OnMinimumSizeMet = () => { },
                OnMinimumSizeNotMet = () => { },
            })).Fill();
        }

        private void ScopeDataToLast30Seconds(DateTime now, int dataIndex)
        {
            for (var j = 0; j < data[dataIndex].Count; j++)
            {
                if (TimeSpan.FromTicks(now.Ticks - (long)data[dataIndex][j].X) > TimeSpan.FromSeconds(30))
                {
                    data[dataIndex].RemoveAt(j--);
                }
            }
        }
    }

    public interface IResource
    {
        string DisplayName { get; }

        float GetSample();

        float? MaxValue { get; }

        ConsoleString GetFormattedSample();
    }

    public class CPUResource : IResource
    {
        public string DisplayName => "CPU Percentage";

        public float? MaxValue => 100;

        private Random rand = new Random();

        private float lastSample;

    
        public ConsoleString GetFormattedSample()
        {
            return (ConsoleMath.Round(lastSample) + " %").ToConsoleString(lastSample < 50 ? ConsoleColor.Green : lastSample < 90 ? ConsoleColor.Yellow : ConsoleColor.Red);
        }

        public float GetSample()
        {
            lastSample = rand.Next(30, 50);
            return lastSample;
        }
    }

    public class RAMResource : IResource
    {
        public string DisplayName => "Available RAM";

        public float? MaxValue => 4;

        private Random rand = new Random();
        private float lastSample;
 
        public ConsoleString GetFormattedSample()
        {
            return (String.Format("{0:n0}", lastSample) + " MB").ToConsoleString(lastSample > 1000 ? ConsoleColor.Green : lastSample > 500 ? ConsoleColor.Yellow : ConsoleColor.Red);
        }

        public float GetSample()
        {
            lastSample = rand.Next(1,4);
            return lastSample;
        }
    }
}

