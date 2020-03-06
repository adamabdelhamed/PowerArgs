using PowerArgs.Samples;
using System;
namespace Samples.NET
{
    class Program
    {
        [STAThread]
        static void Main(string[] args) => new TheSamplesApp().Start().Wait();
    }
}
