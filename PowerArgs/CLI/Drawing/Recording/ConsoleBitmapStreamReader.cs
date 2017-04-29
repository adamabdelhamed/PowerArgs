using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    public class ConsoleBitmapStreamReader
    {
        private ConsoleBitmapFrameSerializer serializer;
        private Stream inputStream;
        private StreamReader reader;
        private TimeSpan? duration;
        private int? frameWidth;
        private int? frameHeight;

        public TimeSpan? Duration => duration;

        public Stream InnerStream => inputStream;

        private ConsoleBitmap readBuffer = null;
        public ConsoleBitmap CurrentBitmap => readBuffer;
        public ConsoleBitmapFrame CurrentFrame { get; private set; }

        public ConsoleBitmapStreamReader(Stream s)
        {
            this.inputStream= s;
            this.reader = new StreamReader(inputStream);
            this.serializer = new ConsoleBitmapFrameSerializer();
            
        }

        public InMemoryConsoleBitmapVideo ReadToEnd(Action<InMemoryConsoleBitmapVideo> progressCallback = null)
        {
            var ret = new InMemoryConsoleBitmapVideo();
            while(ReadFrame().CurrentFrame != null)
            {
                ret.Frames.Add(new InMemoryConsoleBitmapFrame()
                {
                    Bitmap = CurrentBitmap.Clone(),
                    FrameTime = CurrentFrame.Timestamp,
                });

                // This line assumes Duration will be set after the first frame is read
                ret.LoadProgress = CurrentFrame.Timestamp.TotalSeconds / Duration.Value.TotalSeconds;
                ret.Duration = Duration.Value;
                progressCallback?.Invoke(ret);
            }

            return ret;
        }

        public ConsoleBitmapStreamReader ReadFrame()
        {
            if (duration.HasValue == false)
            {
                var lengthbuffer = new byte[sizeof(long)];
                var read = inputStream.Read(lengthbuffer, 0, lengthbuffer.Length);
                if (read != lengthbuffer.Length) throw new FormatException("Could not read length");

                var ticks = BitConverter.ToInt64(lengthbuffer, 0);
                duration = new TimeSpan(ticks);

                var sizeHeader = reader.ReadLine();
                var match = Regex.Match(sizeHeader, @"(?<width>\d+)x(?<height>\d+)");
                if (match.Success == false) throw new FormatException("Could not read size header");

                frameWidth = int.Parse(match.Groups["width"].Value);
                frameHeight = int.Parse(match.Groups["height"].Value);
            }

            var serializedFrame = reader.ReadLine();
            if (serializedFrame == null)
            {
                this.CurrentFrame = null;
                return this;
            }

            var frame = serializer.DeserializeFrame(serializedFrame, frameWidth.Value, frameHeight.Value);
            frame.Paint(ref readBuffer);
            this.CurrentFrame = frame;
            return this;
        }
    }

    public class InMemoryConsoleBitmapVideo
    {
        public double LoadProgress { get; set; }
        public TimeSpan Duration { get; set; }

        public List<InMemoryConsoleBitmapFrame> Frames { get; set; } = new List<InMemoryConsoleBitmapFrame>();

        internal bool TrySeek(TimeSpan destination, out ConsoleBitmap bitmap)
        {
            var i = 0;
            while(i < Frames.Count && Frames[i].FrameTime <= destination)
            {
                i++;
            }

            if(i >= Frames.Count)
            {
                bitmap = null;
                return false;
            }
            else
            {
                bitmap = Frames[i].Bitmap;
                return true;
            }
        }
    }

    public class InMemoryConsoleBitmapFrame
    {
        public TimeSpan FrameTime { get; set; }
        public ConsoleBitmap Bitmap { get; set; }
    }
}
