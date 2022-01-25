using BenchmarkDotNet.Attributes;
using PowerArgs;
using PowerArgs.Cli;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public  class FillRectBenchmark
    {
        private ConsoleBitmap bitmap;
        public FillRectBenchmark()
        {
            bitmap = new ConsoleBitmap(80, 30);
        }

        [Benchmark(Baseline =true)]
        public void FillRectOld()
        {
            bitmap.FillRect(0, 0, bitmap.Width, bitmap.Height);
        }
    }

    [MemoryDiagnoser]
    public class PaintBenchmark
    {
        private ConsoleBitmap[] bitmaps;
        public PaintBenchmark()
        {
            ConsoleProvider.Current = new NoOpConsole();
            bitmaps = new ConsoleBitmap[100];
            for(var i = 0; i < bitmaps.Length; i++)
            {
                bitmaps[i] = new ConsoleBitmap(80, 30);
                bitmaps[i].FillRect(RGB.Red, 0, 0, bitmaps[i].Width, bitmaps[i].Height);
            }
        }

        [Benchmark]
        public void Paint()
        {
            for (var i = 0; i < bitmaps.Length; i++)
            {
                bitmaps[i].Paint();
            }
        }
    }

    public class NoOpConsole : IConsoleProvider
    {
        public bool KeyAvailable => throw new NotImplementedException();

        public ConsoleColor ForegroundColor { get; set; }
        public ConsoleColor BackgroundColor { get; set; }
        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }
        public int BufferWidth { get; set; } = 80;
        public int WindowHeight { get; set; } = 30;
        public int WindowWidth { get; set; } = 80;

        public void Clear()
        {

        }

        public int Read()
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey()
        {
            throw new NotImplementedException();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }

        public void Write(object output)
        {
   
        }

        public void Write(ConsoleString consoleString)
        {

        }

        public void Write(ConsoleCharacter consoleCharacter)
        {
       
        }

        public void WriteLine(object output)
        {
  
        }

        public void WriteLine(ConsoleString consoleString)
        {
        
        }

        public void WriteLine()
        {

        }
    }
}
