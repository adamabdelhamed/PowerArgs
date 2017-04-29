using PowerArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    public class ConsoleBitmapStreamWriter : Lifetime
    {
        private DateTime? firstFrameTime;
        private ConsoleBitmapRawFrame lastFrame;
        private Stream outputStream;
        private StreamWriter writer;
        private ConsoleBitmapFrameSerializer serializer;

        public bool CloseInnerStream { get; set; } = true;
        public Stream InnerStream => outputStream;
        public ConsoleBitmapStreamWriter(Stream s)
        {
            this.outputStream = s;
            this.serializer = new ConsoleBitmapFrameSerializer();
            this.writer = new StreamWriter(s);

            if (s.CanSeek == false)
            {
                throw new ArgumentOutOfRangeException("Stream must be able to seek");
            }
            s.Write(new byte[sizeof(long)], 0, sizeof(long)); // write an empty space for the total time


            this.LifetimeManager.Manage(this.WriteEnd);
        }

        public ConsoleBitmap WriteFrame(ConsoleBitmap bitmap)
        {
            var rawFrame = GetRawFrame(bitmap);

            var now = DateTime.UtcNow;

            if(firstFrameTime.HasValue == false)
            {
                rawFrame.Timestamp = TimeSpan.Zero;
                firstFrameTime = now;
            }
            else
            {
                rawFrame.Timestamp = now - firstFrameTime.Value;
            }

            var frameTime = firstFrameTime.HasValue == false ? TimeSpan.Zero : now - firstFrameTime.Value;

            if (lastFrame == null)
            {
                rawFrame.Timestamp = frameTime;
                StreamHeader(bitmap);
                writer.Write(serializer.SerializeFrame(rawFrame));
            }
            else
            {
                var diff = PrepareDiffFrame(bitmap);
                diff.Timestamp = frameTime;
                if(diff.Diffs.Count > bitmap.Width * bitmap.Height / 2)
                {
                    writer.Write(serializer.SerializeFrame(rawFrame));
                }
                else if(diff.Diffs.Count > 0)
                {
                    writer.Write(serializer.SerializeFrame(diff));
                }
            }

            lastFrame = rawFrame;
            return bitmap;
        }

        private void WriteEnd()
        {
            writer.Flush();
            outputStream.Position = 0;
            var recordingTicks = lastFrame.Timestamp.Ticks;
            var bytes = BitConverter.GetBytes(recordingTicks);
            outputStream.Write(bytes, 0, bytes.Length);

            writer.Flush();

            if (CloseInnerStream)
            {
                outputStream.Close();
            }
        }


        private void StreamHeader(ConsoleBitmap initialFrame)
        {
            writer.WriteLine($"{initialFrame.Width}x{initialFrame.Height}");
        }

      

        private ConsoleBitmapRawFrame GetRawFrame(ConsoleBitmap bitmap)
        {
            var rawFrame = new ConsoleBitmapRawFrame();
            rawFrame.Pixels = new ConsoleCharacter[bitmap.Width][];
            for (int x = 0; x < bitmap.Width; x++)
            {
                rawFrame.Pixels[x] = new ConsoleCharacter[bitmap.Height];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    var pixelValue = bitmap.GetPixel(x, y).Value.HasValue ? bitmap.GetPixel(x, y).Value.Value : new ConsoleCharacter(' ');
                    rawFrame.Pixels[x][y] = pixelValue;
                }
            }
            return rawFrame;
        }

        private ConsoleBitmapDiffFrame PrepareDiffFrame(ConsoleBitmap bitmap)
        {
            ConsoleBitmapDiffFrame diff = new ConsoleBitmapDiffFrame();
            diff.Diffs = new List<ConsoleBitmapPixelDiff>();
            int changes = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.HasChanged)
                    {
                        changes++;
                        if (pixel.Value.HasValue)
                        {
                            diff.Diffs.Add(new ConsoleBitmapPixelDiff()
                            {
                                X = x,
                                Y = y,
                                Value = pixel.Value.Value
                            });
                        }
                    }
                }
            }

            return diff;
        }
    }
}
