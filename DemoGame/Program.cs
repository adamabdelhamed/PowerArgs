using PowerArgs.Games;
using PowerArgs.Cli;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace DemoGame
{
    class Program
    {
        public static void Main(string[] args) => Args.InvokeMain<Prog>(args);
    }

    public class Item
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }

    public class SlowList : CachedRemoteList<Item>
    {
        private List<Item> items;
        public SlowList(List<Item> items)
        {
            this.items = items;
        }

        protected override Promise<int> FetchCountAsync()
        {
            var d = Deferred<int>.Create();
            new Thread(() =>
            {
                Thread.Sleep(1000);
                d.Resolve(items.Count);
            }).Start();
            return d.Promise;
        }

        protected override Promise<List<Item>> FetchRangeAsync(int min, int count)
        {
            var d = Deferred<List<Item>>.Create();
            new Thread(() =>
            {
                Thread.Sleep(1000);
                d.Resolve(items.Skip(min).Take(count).ToList());
            }).Start();
            return d.Promise;
        }
    }


    class Prog
    { 
        public void Main()
        {
            var items = new List<Item>();

            for(var i = 0; i < 100; i++)
            {
                items.Add(new Item()
                {
                    Bar= "Bar"+i,
                    Foo = "Foo"+i,
                });
            }

            var app = new ConsoleApp();
            var dataGrid = new ListGrid<Item>(new ListGridOptions<Item>()
            {
                SelectionMode = DataGridSelectionMode.Row,
                DataSource = new SlowList(items),
                Columns = new List<ListGridColumnDefinition<Item>>()
                {
                    new ListGridColumnDefinition<Item>()
                    {
                        Header = "Foo".ToGreen(),
                        Width= .25,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Foo.ToConsoleString() }
                    },
                    new ListGridColumnDefinition<Item>()
                    {
                        Header = "Bar".ToRed(),
                        Width = .25,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Bar.ToConsoleString() }
                    },
                      new ListGridColumnDefinition<Item>()
                    {
                        Header = "Foo2".ToGreen(),
                        Width= .15,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Foo.ToConsoleString() }
                    },
                    new ListGridColumnDefinition<Item>()
                    {
                        Header = "Bar2".ToRed(),
                        Width = .15,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Bar.ToConsoleString() }
                    },
                    new ListGridColumnDefinition<Item>()
                    {
                        Header = "Foo3".ToGreen(),
                        Width= .15,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Foo.ToConsoleString() }
                    },
                    new ListGridColumnDefinition<Item>()
                    {
                        Header = "Bar3".ToRed(),
                        Width = .15,
                        Type = GridValueType.Percentage,
                        Formatter = (item) => new Label(){ Text = item.Bar.ToConsoleString() }
                    }
                }
            });
            app.LayoutRoot.Add(dataGrid).Fill();
            app.Start().Wait();
            return;
            var winSound = new WindowsSoundProvider.SoundProvider();
            Sound.Provider = winSound;
            winSound.StartPromise.Wait();
            new DemoMultiPlayerGameApp().Start().Wait();
            Sound.Dispose();
        }
    }
}
