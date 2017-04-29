using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public abstract class ConsoleBitmapFrame
    {
        public TimeSpan Timestamp { get; set; }
        public abstract ConsoleBitmap Paint(ref ConsoleBitmap bitmap);
    }

    public class ConsoleBitmapRawFrame : ConsoleBitmapFrame
    {
        public ConsoleCharacter[][] Pixels { get; set; }

        public override ConsoleBitmap Paint(ref ConsoleBitmap bitmap)
        {
            bitmap = bitmap ?? new ConsoleBitmap(0, 0, Pixels.Length, Pixels[0].Length);
            for(var x = 0; x < Pixels.Length; x++)
            {
                for (var y = 0; y < Pixels[0].Length; y++)
                {
                    bitmap.Pen = Pixels[x][y];
                    bitmap.DrawPoint(x, y);
                }
            }
            return bitmap;
        }
    }

    public class ConsoleBitmapDiffFrame : ConsoleBitmapFrame
    {
        public List<ConsoleBitmapPixelDiff> Diffs { get; set; }

        public override ConsoleBitmap Paint(ref ConsoleBitmap bitmap)
        {
            foreach (var diff in Diffs)
            {
                bitmap.Pen = diff.Value;
                bitmap.DrawPoint(diff.X, diff.Y);
            }
            return bitmap;
        }
    }

    public class ConsoleBitmapPixelDiff
    {
        public int X { get; set; }
        public int Y { get; set; }
        public ConsoleCharacter Value { get; set; }
    }
}
