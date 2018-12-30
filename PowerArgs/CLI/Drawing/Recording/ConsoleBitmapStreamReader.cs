using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An object that can read console bitmap frames from a stream
    /// </summary>
    public class ConsoleBitmapStreamReader
    {
        private ConsoleBitmapFrameSerializer serializer;
        private Stream inputStream;
        private StreamReader reader;
        private TimeSpan? duration;
        private int? frameWidth;
        private int? frameHeight;

        /// <summary>
        /// The duration of the video, only known once the first frame is read
        /// </summary>
        public TimeSpan? Duration => duration;

        /// <summary>
        /// The inner stream that was passed to the constructor
        /// </summary>
        public Stream InnerStream => inputStream;

        /// <summary>
        /// A bitmap that represents the most recently read frame
        /// </summary>
        private ConsoleBitmap readBuffer = null;

        /// <summary>
        /// The most recently read frame bitmap
        /// </summary>
        public ConsoleBitmap CurrentBitmap => readBuffer;

        /// <summary>
        /// The most recently read frame data
        /// </summary>
        public ConsoleBitmapFrame CurrentFrame { get; private set; }

        /// <summary>
        /// Creates a new reader from a given stream
        /// </summary>
        /// <param name="s">a stream to read</param>
        public ConsoleBitmapStreamReader(Stream s)
        {
            this.inputStream= s;
            this.reader = new StreamReader(inputStream);
            this.serializer = new ConsoleBitmapFrameSerializer();
            
        }

        /// <summary>
        /// Reads the stream to the end, providing progress information along the way
        /// </summary>
        /// <param name="progressCallback">a callback that will be called each time there is a new frame available</param>
        /// <returns>The complete video in its in fully expanded in memory structure</returns>
        public InMemoryConsoleBitmapVideo ReadToEnd(Action<InMemoryConsoleBitmapVideo> progressCallback = null)
        {
            var ret = new InMemoryConsoleBitmapVideo();
            while(ReadFrame().CurrentFrame != null)
            {
                ret.Frames.Add(new InMemoryConsoleBitmapFrame()
                {
                    Bitmap = CurrentBitmap.Clone(), // clone the current frame since we paint over it every time
                    FrameTime = CurrentFrame.Timestamp,
                });

                // Duration will be set after the first frame is read so no need to check HasValue
                ret.LoadProgress = CurrentFrame.Timestamp.TotalSeconds / Duration.Value.TotalSeconds;
                ret.Duration = Duration.Value; // proxy the known duration to the video object so the progress callback can get at it
                progressCallback?.Invoke(ret);
            }

            return ret;
        }

        /// <summary>
        /// Reads an individual frame from the stream.  CurrentFrame will be set to the current frame data or null if
        /// there are no more frames to read.  
        /// </summary>
        /// <returns>This reader</returns>
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

    /// <summary>
    /// The fully expanded in memory representation of a console bitmap video
    /// </summary>
    public class InMemoryConsoleBitmapVideo
    {
        /// <summary>
        /// The loading progress, from 0 to 1
        /// </summary>
        public double LoadProgress { get; set; }

        /// <summary>
        /// The duration of the video, populated once the first frame is read
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// All frames that have been loaded so far
        /// </summary>
        public List<InMemoryConsoleBitmapFrame> Frames { get; set; } = new List<InMemoryConsoleBitmapFrame>();

        /// <summary>
        /// Tries to seek to the requested destination in the video.
        /// </summary>
        /// <param name="destination">the timestamp to seek to</param>
        /// <param name="bitmap">the bitmap reference to update</param>
        /// <param name="startFrameIndex">the first frame index to look into or 0 if starting from the beginning</param>
        /// <returns>the frame index of the loaded frame or -1 if the destination has not yet loaded</returns>
        public int Seek(TimeSpan destination, out ConsoleBitmap bitmap, int startFrameIndex = 0)
        {
            if (Frames.Count == 0) throw new InvalidOperationException("This video has no frames");
            if(Frames.Count == 1)
            {
                bitmap = Frames[0].Bitmap;
                return 0;
            }

            int i;
            for (i = startFrameIndex; i < Frames.Count; i++)
            {
                var currentFrame = Frames[i];

                if(currentFrame.FrameTime == destination)
                {
                    bitmap = currentFrame.Bitmap;
                }
                else if(currentFrame.FrameTime > destination)
                {
                    i = i == 0 ? 0 : i - 1;
                    bitmap = Frames[i].Bitmap;
                    return i;
                }
            }
            if(LoadProgress < 1)
            {
                bitmap = null;
                return -1;
            }

            i = Frames.Count - 1;
            bitmap = Frames[i].Bitmap;
            return i;
        }
    }

    /// <summary>
    /// The fully expanded representation of an in memory video frame
    /// </summary>
    public class InMemoryConsoleBitmapFrame
    {
        /// <summary>
        /// The frame's timestamp
        /// </summary>
        public TimeSpan FrameTime { get; set; }

        /// <summary>
        /// The frame image
        /// </summary>
        public ConsoleBitmap Bitmap { get; set; }
    }
}
