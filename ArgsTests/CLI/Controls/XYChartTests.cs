using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    public class XYChartTests
    {
        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public void CPUChartPastDay()
        {
            var options = new XYChartOptions()
            {
                Title = "CPU Percentage (past day)".ToYellow(),
                XAxisFormatter = new DateTimeFormatter(),
                YMinOverride = 0,
                YMaxOverride = 100,
                Data = new List<Series>()
                    {
                        new Series()
                        {
                            Title = "",
                            Points = new List<DataPoint>()
                            {
                                new DataPoint(){ X = new DateTime(2000,1,1).Ticks, Y = 50 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(3).Ticks, Y = 45 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(6).Ticks, Y = 50 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(9).Ticks, Y = 55 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(12).Ticks, Y = 50 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(15).Ticks, Y = 45 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(18).Ticks, Y = 50 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(21).Ticks, Y = 55 },
                                new DataPoint(){ X = new DateTime(2000,1,1).AddHours(24).Ticks, Y = 50 },
                            }
                        }
                    }
            };

            RenderChartTestCommon(options);
        }

        [TestMethod]
        public void DistanceOverTime()
        {
            var options = new XYChartOptions()
            {
                Title = "Distance over time".ToYellow(),
                XAxisFormatter = new TimeSpanFormatter(),
                Data = new List<Series>()
                {
                    new Series()
                    {
                        Title = "",
                        Points = new List<DataPoint>()
                        {
                            new DataPoint(){ X = TimeSpan.Zero.Ticks, Y = 0 },
                            new DataPoint(){ X = TimeSpan.Zero.Add(TimeSpan.FromDays(1)).Ticks, Y = 1 },
                            new DataPoint(){ X = TimeSpan.Zero.Add(TimeSpan.FromDays(2)).Ticks, Y = 2 },
                            new DataPoint(){ X = TimeSpan.Zero.Add(TimeSpan.FromDays(3)).Ticks, Y = 3 },
                            new DataPoint(){ X = TimeSpan.Zero.Add(TimeSpan.FromDays(4)).Ticks, Y = 4 },
                        }
                    }
                }
            };

            RenderChartTestCommon(options);
        }

        [TestMethod]
        public void NPSPast3Months()
        {
            var points = new List<DataPoint>();

            DateTime end = new DateTime(2018, 1, 1);
            DateTime current = end.AddDays(-90);

            var npsValues = new double[] { 32, 33, 33.6, 33.8, 34, 34.5, 35, 35, 36, 36.5, 37, 37, 37.5 };

            int npsIndex = 0;
            while(current <= end)
            {
                points.Add(new DataPoint()
                {
                    X = current.Ticks,
                    Y = npsValues[npsIndex++]
                });

                current = current.AddDays(7);
                if (npsIndex == npsValues.Length) npsIndex = 0;
            }

            var options = new XYChartOptions()
            {
                Title = "NPS (past 90 days)".ToYellow(),
                XAxisFormatter = new DateTimeFormatter(),
                Data = new List<Series>() { new Series() { Title = "", Points = points } }
            };

            RenderChartTestCommon(options);
        }

        [TestMethod]
        public void Parabola()
        {

            var points = new List<DataPoint>();
            for(var i = -100; i <= 100; i++)
            {
                points.Add(new DataPoint()
                {
                    X = i,
                    Y = i * i
                });
            }

            RenderChartTestCommon(new XYChartOptions()
            {
                Title = "Parabola".ToRed(),
                Data = new List<Series>() { new Series() { Points = points } }
            });
        }

        [TestMethod]
        public void Cube()
        {

            var points = new List<DataPoint>();
            for (var i = -100; i <= 100; i++)
            {
                points.Add(new DataPoint()
                {
                    X = i,
                    Y = i * i * i
                });
            }

            RenderChartTestCommon(new XYChartOptions()
            {
                Title = "Cube".ToMagenta(),
                Data = new List<Series>() { new Series() { Points = points } }
            });
        }



        public void RenderChartTestCommon(XYChartOptions options, int w = 80, int h = 30)
        {
            var app = new CliTestHarness(this.TestContext, w, h);
            app.QueueAction(() => app.LayoutRoot.Add(new XYChart(options)).Fill());
            app.QueueAction(app.Stop);
            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
