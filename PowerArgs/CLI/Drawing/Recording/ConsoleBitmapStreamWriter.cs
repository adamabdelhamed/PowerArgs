using PowerArgs.Cli.Physics;
using System.Text;

namespace PowerArgs.Cli;
/// <summary>
/// An object that can write console bitmap video data to a stream
/// </summary>
public class ConsoleBitmapVideoWriter
{
    public const int DurationLineLength = 30;
    private DateTime? firstFrameTime;
    private ConsoleBitmapRawFrame lastFrame;
    private ConsoleBitmapFrameSerializer serializer;
    private TimeSpan TotalPauseTime = TimeSpan.Zero;
    private DateTime? pausedAt = null;

    public RectF? Window { get; set; }

    private int GetEffectiveLeft => Window.HasValue ? (int)Window.Value.Left : 0;
    private int GetEffectiveTop => Window.HasValue ? (int)Window.Value.Top : 0;
    private int GetEffectiveWidth(ConsoleBitmap bitmap) => Window.HasValue ? (int)Window.Value.Width : bitmap.Width;
    private int GetEffectiveHeight(ConsoleBitmap bitmap) => Window.HasValue ? (int)Window.Value.Height : bitmap.Height;

    private bool isFinished;

    public bool IsFinished => isFinished;

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


    private int bufferIndex;
    private char[] buffer = new char[250000];

    private Action<string> finishAction;
    /// <summary>
    /// Creates a new writer given a stream
    /// </summary>
    /// <param name="s">the stream to write to</param>
    public ConsoleBitmapVideoWriter(Action<string> finishAction)
    {
        this.finishAction = finishAction;
        this.serializer = new ConsoleBitmapFrameSerializer();
        for (var i = 0; i < DurationLineLength - 1; i++)
        {
            Append("-");
        }
        Append("\n");
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
        if (isFinished) throw new NotSupportedException("Already finished");



        if (pausedAt.HasValue) return bitmap;

        var rawFrame = GetRawFrame(bitmap);

        var now = DateTime.UtcNow - TotalPauseTime;

        if (firstFrameTime.HasValue == false)
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
            Append(serializer.SerializeFrame(rawFrame));
            FramesWritten++;
        }
        else
        {
            if (GetEffectiveWidth(bitmap) != lastFrame.Pixels.Length || GetEffectiveHeight(bitmap) != lastFrame.Pixels[0].Length)
            {
                throw new InvalidOperationException("Video frame has changed size");
            }

            var diff = PrepareDiffFrame(lastFrame, bitmap);
            diff.Timestamp = rawFrame.Timestamp;

            var numPixels = GetEffectiveWidth(bitmap) * GetEffectiveHeight(bitmap);
            if (force || diff.Diffs.Count > numPixels / 2)
            {
                var frame = serializer.SerializeFrame(rawFrame);
                //checking to make sure we can deserialize what we just wrote so that if we can't
                // we still have time to debug. I'd love to get rid of this check for perf, but
                // there have been some cases where I wasn't able to read back what was written and if 
                // that edge case creeps up I want to catch it early.
                var deserialized = serializer.DeserializeFrame(frame, bitmap.Width, bitmap.Height);
                var frameBack = serializer.SerializeFrame((ConsoleBitmapRawFrame)deserialized);
                if (frameBack.Equals(frame) == false)
                {
                    throw new Exception("Serialization failure");
                }
                if (frame.EndsWith("\n") == false)
                {
                    throw new Exception();
                }
                Append(frame);
                FramesWritten++;
            }
            else if (diff.Diffs.Count > 0)
            {
                Append(serializer.SerializeFrame(diff));
                FramesWritten++;
            }
        }

        lastFrame = rawFrame;
        return bitmap;
    }

    private void Append(string s)
    {
        char[] temp = null;
        if (buffer.Length < bufferIndex + s.Length)
        {
            temp = new char[Math.Max(buffer.Length * 2, bufferIndex + s.Length * 2)];
            Array.Copy(buffer, 0, temp, 0, bufferIndex);
            buffer = temp;
        }

        for (var i = 0; i < s.Length; i++)
        {
            buffer[bufferIndex++] = s[i];
        }
    }

    private void AppendLine(string s)
    {
        Append(s);
        Append("\n");
    }

    public bool TryFinish()
    {
        if (isFinished) return false;
        Finish();
        return true;
    }

    /// <summary>
    /// Writes the duration information in the beginning of the stream and then closes the inner stream
    /// if CloseInnerStream is true
    /// </summary>
    public void Finish()
    {
        if (isFinished)
        {
            throw new Exception("Already finished");
        }
        var toPrepend = CalculateDurationString();
        for (var i = 0; i < toPrepend.Length; i++)
        {
            buffer[i] = toPrepend[i];
        }
        var str = new string(buffer, 0, bufferIndex).Trim();
        buffer = null;
        finishAction(str);
    }

    private string CalculateDurationString()
    {
        var recordingTicks = lastFrame.Timestamp.Ticks;
        var ticksString = recordingTicks.ToString() + "\n";

        while (ticksString.Length < DurationLineLength)
        {
            ticksString = "0" + ticksString;
        }

        return ticksString;
    }

    private void StreamHeader(ConsoleBitmap initialFrame)
    {
        AppendLine($"{GetEffectiveWidth(initialFrame)}x{GetEffectiveHeight(initialFrame)}");
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
                var pixelValue = pixel.Value;
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

                if (pixel.HasChanged || hasPreviousPixel == false || (pixel.Value.Equals(previousPixel) == false))
                {
                    changes++;
                    diff.Diffs.Add(new ConsoleBitmapPixelDiff()
                    {
                        X = x,
                        Y = y,
                        Value = pixel.Value
                    });
                }
            }
        }

        return diff;
    }
}

