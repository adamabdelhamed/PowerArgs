using BenchmarkDotNet.Attributes;
using PowerArgs;
using PowerArgs.Cli;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class FillRectBenchmark
    {
        private ConsoleBitmap bitmap;
        public FillRectBenchmark()
        {
            bitmap = new ConsoleBitmap(80, 30);
        }

        [Benchmark(Baseline = true)]
        public void FillRect()
        {
            bitmap.FillRect(0, 0, bitmap.Width, bitmap.Height);
        }
    }

    [MemoryDiagnoser]
    public class PaintBenchmark
    {
        private ConsoleBitmap[] continuousBitmaps;
        private ConsoleBitmap[] aFewShapesBitmaps;
        private ConsoleBitmap[] worstCasebitmaps;
        public PaintBenchmark()
        {
            ConsoleProvider.Current = new NoOpConsole();
            InitPlainCase();
            InitFewShapesCase();
            InitWorstCase();
        }

        private void InitPlainCase()
        {
            continuousBitmaps = new ConsoleBitmap[100];
            for (var i = 0; i < continuousBitmaps.Length; i++)
            {
                continuousBitmaps[i] = new ConsoleBitmap(80, 30);
                continuousBitmaps[i].FillRect(RGB.Red, 0, 0, continuousBitmaps[i].Width, continuousBitmaps[i].Height);
            }
        }

        private void InitFewShapesCase()
        {
            aFewShapesBitmaps = new ConsoleBitmap[100];
            for (var i = 0; i < aFewShapesBitmaps.Length; i++)
            {
                aFewShapesBitmaps[i] = new ConsoleBitmap(80, 30);
                aFewShapesBitmaps[i].FillRect(RGB.Red, 0, 0, 10, 5);
                aFewShapesBitmaps[i].FillRect(RGB.Green, 10, 10, 10, 5);
                aFewShapesBitmaps[i].FillRect(RGB.DarkYellow, 13, 12, 10, 5);
            }
        }

        private void InitWorstCase()
        {
            worstCasebitmaps = new ConsoleBitmap[100];
            var on = true;
            for (var i = 0; i < continuousBitmaps.Length; i++)
            {
                worstCasebitmaps[i] = new ConsoleBitmap(80, 30);

                for (var x = 0; x < worstCasebitmaps[i].Width; x++)
                {
                    for (var y = 0; y < worstCasebitmaps[i].Height; y++)
                    {
                        on = !on;
                        worstCasebitmaps[i].DrawPoint(new ConsoleCharacter('X', on ? RGB.Red : RGB.Green, on ? RGB.Black : RGB.Cyan), x, y);
                    }
                }
            }
        }



        [Benchmark(OperationsPerInvoke = 100)]
        public void PaintContinuius()
        {
            for (var i = 0; i < continuousBitmaps.Length; i++)
            {
                continuousBitmaps[i].Paint();
            }
        }


        [Benchmark(OperationsPerInvoke = 100)]
        public void PaintFewShapes()
        {
            for (var i = 0; i < aFewShapesBitmaps.Length; i++)
            {
                aFewShapesBitmaps[i].Paint();
            }
        }

        [Benchmark(OperationsPerInvoke = 100)]
        public void PaintWorst()
        {
            for (var i = 0; i < worstCasebitmaps.Length; i++)
            {
                worstCasebitmaps[i].Paint();
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

        public void Write(char[] buffer, int length)
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
