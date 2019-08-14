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
        private static readonly ConsoleCharacter defaultChar = new ConsoleCharacter(' ');
        public const int DurationLineLength = 30;
        private DateTime? firstFrameTime;
        private ConsoleBitmapRawFrame lastFrame;
        private Stream outputStream;
        private StreamWriter writer;
        private ConsoleBitmapFrameSerializer serializer;

        private TimeSpan TotalPauseTime = TimeSpan.Zero;
        private DateTime? pausedAt = null;

        public Rectangle? Window { get; set; }

        private int GetEffectiveLeft => Window.HasValue ? Window.Value.Left : 0;
        private int GetEffectiveTop => Window.HasValue ? Window.Value.Top : 0;
        private int GetEffectiveWidth(ConsoleBitmap bitmap) =>  Window.HasValue ? Window.Value.Width : bitmap.Width;
        private int GetEffectiveHeight(ConsoleBitmap bitmap) => Window.HasValue ? Window.Value.Height : bitmap.Height;


        public void Pause()
        {
            if (pausedAt.HasValue) return;
            pausedAt = DateTime.UtcNow;
        }

        public void Resume()
        {
            if (pausedAt.HasValue == false) return;
            var now = DateTime.UtcNow;
            TotalPauseTime += now - pausedAt.Value;
            pausedAt = null;
        }

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
            s.Write(new byte[DurationLineLength], 0, DurationLineLength); // write an empty space for the total time + newline


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
            if (pausedAt.HasValue) return bitmap;

            var rawFrame = GetRawFrame(bitmap);

            var now = DateTime.UtcNow - TotalPauseTime;

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
                if(GetEffectiveWidth(bitmap) != lastFrame.Pixels.Length || GetEffectiveHeight(bitmap) != lastFrame.Pixels[0].Length)
                {
                    throw new InvalidOperationException("Video frame has changed size");
                }

                var diff = PrepareDiffFrame(lastFrame, bitmap);
                diff.Timestamp = rawFrame.Timestamp;

                var numPixels = GetEffectiveWidth(bitmap) * GetEffectiveHeight(bitmap);
                if (force || diff.Diffs.Count > numPixels / 2)
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
            var ticksString = recordingTicks.ToString() + writer.NewLine;

            while(ticksString.Length < DurationLineLength)
            {
                ticksString = "0" + ticksString;
            }

            var bytes = writer.Encoding.GetBytes(ticksString);
            if (bytes.Length != DurationLineLength) throw new InvalidDataException("Duration header error");
            outputStream.Write(bytes, 0, bytes.Length);

            if (CloseInnerStream)
            {
                outputStream.Close();
            }
        }

        
        private void StreamHeader(ConsoleBitmap initialFrame)
        {
            writer.WriteLine($"{GetEffectiveWidth(initialFrame)}x{GetEffectiveHeight(initialFrame)}");
        }

        private ConsoleBitmapRawFrame GetRawFrame(ConsoleBitmap bitmap)
        {
            var rawFrame = new ConsoleBitmapRawFrame();
            rawFrame.Pixels = new ConsoleCharacter[GetEffectiveWidth(bitmap)][];
            for (int x = 0; x < GetEffectiveWidth(bitmap); x++)
            {
                rawFrame.Pixels[x] = new ConsoleCharacter[GetEffectiveHeight(bitmap)];
                for (int y = 0; y < GetEffectiveHeight(bitmap); y++)
                {
                    var pixel = bitmap.GetPixel(GetEffectiveLeft + x, GetEffectiveTop + y);
                    var pixelValue = pixel.Value.HasValue ? pixel.Value.Value : defaultChar;
                    rawFrame.Pixels[x][y] = pixelValue;
                }
            }
            return rawFrame;
        }

        private ConsoleBitmapDiffFrame PrepareDiffFrame(ConsoleBitmapRawFrame previous, ConsoleBitmap bitmap)
        {
            ConsoleBitmapDiffFrame diff = new ConsoleBitmapDiffFrame();
            diff.Diffs = new List<ConsoleBitmapPixelDiff>();
            int changes = 0;
            for (int y = 0; y < GetEffectiveHeight(bitmap); y++)
            {
                for (int x = 0; x < GetEffectiveWidth(bitmap); x++)
                {
                    var pixel = bitmap.GetPixel(GetEffectiveLeft + x, GetEffectiveTop + y);
                    var hasPreviousPixel = previous.Pixels.Length == GetEffectiveWidth(bitmap) && previous.Pixels[0].Length == GetEffectiveHeight(bitmap);
                    var previousPixel = hasPreviousPixel ? previous.Pixels[x][y] : default(ConsoleCharacter);

                    if (pixel.HasChanged || hasPreviousPixel == false || (pixel.Value.HasValue && pixel.Value.Value.Equals(previousPixel) == false))
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
