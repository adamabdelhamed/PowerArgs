using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class CliProfiler : ObservableObject
    {
        private static Lazy<CliProfiler> _instance = new Lazy<CliProfiler>(()=> new CliProfiler());
        private Dictionary<string, List<TimeSpan>> timeSamples;
        public static CliProfiler Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public int PaintMessagesQueued { get { return Get<int>(); } set { Set(value); } }



        public int PaintMessagesProcessed { get { return Get<int>(); } set { Set(value); } }

        public int TotalMessagesQueued { get { return Get<int>(); } set { Set(value); } }
        public int TotalMessagesProcessed { get { return Get<int>(); } set { Set(value); } }

        public int TotalNonIdleIterations { get { return Get<int>(); } set { Set(value); } }

        public double MessagesProcessedPerIteration
        {
            get
            {
                if (TotalNonIdleIterations == 0) return 0;
                return (double)TotalMessagesProcessed / TotalNonIdleIterations;
            }
        }

        private CliProfiler()
        {
            timeSamples = new Dictionary<string, List<TimeSpan>>();
        }

        public void AddTimeSample(string key, TimeSpan elapsed)
        {
            List<TimeSpan> samples;
            if(timeSamples.TryGetValue(key, out samples) == false)
            {
                samples = new List<TimeSpan>();
                timeSamples.Add(key, samples);
            }
            samples.Add(elapsed);
        }

        public void Dump(string file)
        {
            var ret = "";
            foreach (var prop in GetType().GetProperties())
            {
                ret += prop.Name + " = " + prop.GetValue(this) + Environment.NewLine;
            }

            if (timeSamples.Count > 0)
            {
                ret += "\nTime Samples"+Environment.NewLine+Environment.NewLine;
                foreach (var key in timeSamples.Keys)
                {
                    var avg = timeSamples[key].Average(t => t.TotalMilliseconds);
                    var min = timeSamples[key].Min(t => t.TotalMilliseconds);
                    var max = timeSamples[key].Max(t => t.TotalMilliseconds);
                    ret += $"{key} (AVG: {avg}) (MIN: {min}) (MAX: {max})"+Environment.NewLine+Environment.NewLine;

                    foreach(var sample in timeSamples[key])
                    {
                        ret += "    "+(int)sample.TotalMilliseconds+" ms"+Environment.NewLine;
                    }
                }
            }

            File.WriteAllText(file, ret);
        }
    }

    public class TimeProfiler : IDisposable
    {
        Stopwatch sw;
        string key;
        public TimeProfiler(string key)
        {
            sw = new Stopwatch();
            this.key = key;
            sw.Start();
        }

        ~TimeProfiler()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                sw.Stop();
                CliProfiler.Instance.AddTimeSample(key, sw.Elapsed);
            }
        }
    }
}
