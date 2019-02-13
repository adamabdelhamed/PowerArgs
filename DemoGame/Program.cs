using PowerArgs.Games;
using PowerArgs.Cli;
using PowerArgs;
using System;

namespace DemoGame
{
    class Program
    {
        public static void Main(string[] args) => Args.InvokeMain<Prog>(args);
    }

    class Prog
    { 
        public void Main()
        {


            XYChart.Show(new XYChartOptions()
            {
                Title = "MSFT Stock Price".ToGreen(),
                XAxisFormatter = new DateTimeFormatter(),
                Data = new System.Collections.Generic.List<Series>()
                {
                    new Series()
                    {
                        Points = new System.Collections.Generic.List<DataPoint>()
                        {
                            new DataPoint(){ X = DateTime.Today.AddDays(-90).Ticks, Y = 100 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-80).Ticks, Y = 110 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-70).Ticks, Y = 105 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-60).Ticks, Y = 100 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-50).Ticks, Y = 90 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-40).Ticks, Y = 95 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-30).Ticks, Y = 103 },
                            new DataPoint(){ X = DateTime.Today.AddDays(-20).Ticks, Y = 110 },
                        },
                        PlotMode = PlotMode.Lines
                    }
                }
            });


            var winSound = new WindowsSoundProvider.SoundProvider();
            Sound.Provider = winSound;
            winSound.StartPromise.Wait();
            new DemoMultiPlayerGameApp().Start().Wait();
            Sound.Dispose();
        }
    }
}
