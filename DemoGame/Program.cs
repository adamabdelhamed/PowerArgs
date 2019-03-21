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
            var winSound = new WindowsSoundProvider.SoundProvider();
            Sound.Provider = winSound;
            winSound.StartPromise.Wait();
            new DemoMultiPlayerGameApp().Start().Wait();
            Sound.Dispose();
        }
    }
}
