using BenchmarkDotNet.Attributes;
using PowerArgs;
using PowerArgs.Cli;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ArrayBenchmark
    {
        private ConsoleCharacter[] pixels1D;
        private ConsoleCharacter[][] pixels2D;
        public ArrayBenchmark()
        {
            pixels1D = new ConsoleCharacter[100*100];
            pixels2D = new ConsoleCharacter[100][];
            for(var x = 0; x < 100; x++)
            {
                pixels2D[x] = new ConsoleCharacter[100];
            }
        }

        [Benchmark]
        public void FillRect1D()
        {
            var span = pixels1D.AsSpan();
            for( var x = 0; x < 100; x++)
            {
                for (var y = 0; y < 100; y++)
                {
                    var i = y * 100 + x;
                    span[i] = new ConsoleCharacter(' ');
                }
            }
        }

        [Benchmark]
        public void FillRect1DWhole()
        {
            var span = pixels1D.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = new ConsoleCharacter(' ');
            }
        }

        [Benchmark]
        public void FillRect2D()
        {
            var spanX = pixels2D.AsSpan();
            for (var x = 0; x < 100; x++)
            {
                var spanY = spanX[x].AsSpan();
                for (var y = 0; y < 100; y++)
                {
                    spanY[y] = new ConsoleCharacter(' ');
                }
            }
        }
    }
}
