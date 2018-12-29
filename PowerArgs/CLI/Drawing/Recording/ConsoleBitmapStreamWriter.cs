using System;
using System.Collections.Generic;
using System.IO;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An object that can write console bitmap video data to a stream
    /// </summary>
    public class ConsoleBitmapStreamWriter : Lifetime
    {
        private DateTime? firstFrameTime;
        private ConsoleBitmapRawFrame lastFrame;
        private Stream outputStream;
        private StreamWriter writer;
        private ConsoleBitmapFrameSerializer serializer;

        /// <summary>
        /// Gets the total number of frames written by the writer. This only counts unique frames
        /// since calls to write frames with the same image as the previous frame are ignored.
        /// </summary>
        public int FramesWritten { get; private set; } = 0;

        /// <summary>
        /// If true then the writer will close the inner stream when it's finished
        /// </summary>
        public bool CloseInnerStream { get; set; } = true;

        /// <summary>
        /// Gets the inner stream that was passed to the constructor
        /// </summary>
        public Stream InnerStream => outputStream;

        /// <summary>
        /// Creates a new writer given a stream
        /// </summary>
        /// <param name="s">the stream to write to</param>
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


            this.OnDisposed(this.WriteEnd);
        }

        /// <summary>
        /// Writes the given bitmap image as a frame to the stream.  If this is the first image or more than half of the pixels have
        /// changed then a raw frame will be written.   Otherwise, a diff frame will be written.
        /// 
        /// This method uses the system's wall clock to determine the timestamp for this frame. The timestamp will be 
        /// relative to the wall clock time when the first frame was written.
        /// </summary>
        /// <param name="bitmap">the image to write</param>
        /// <param name="desiredFrameTime">if provided, sstamp the frame with this time, otherwise stamp it with the wall clock delta from the first frame time</param>
        /// <param name="force">if true, writes the frame even if there are no changes</param>
        /// <returns>the same bitmap that was passed in</returns>
        public ConsoleBitmap WriteFrame(ConsoleBitmap bitmap, bool force = false, TimeSpan? desiredFrameTime = null)
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
                rawFrame.Timestamp = desiredFrameTime.HasValue ? desiredFrameTime.Value : now - firstFrameTime.Value;
            }
            
            if (lastFrame == null)
            {
                StreamHeader(bitmap);
                writer.Write(serializer.SerializeFrame(rawFrame));
                FramesWritten++;
            }
            else
            {
                var diff = PrepareDiffFrame(bitmap);
                diff.Timestamp = rawFrame.Timestamp;
                if(force || diff.Diffs.Count > bitmap.Width * bitmap.Height / 2)
                {
                    writer.Write(serializer.SerializeFrame(rawFrame));
                    FramesWritten++;
                }
                else if(diff.Diffs.Count > 0)
                {
                    writer.Write(serializer.SerializeFrame(diff));
                    FramesWritten++;
                }
            }

            lastFrame = rawFrame;
            return bitmap;
        }

        /// <summary>
        /// Writes the duration information in the beginning of the stream and then closes the inner stream
        /// if CloseInnerStream is true
        /// </summary>
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
