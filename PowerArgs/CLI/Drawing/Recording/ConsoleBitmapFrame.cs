using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// The base class for a console bitmap frame
    /// </summary>
    public abstract class ConsoleBitmapFrame
    {
        /// <summary>
        /// The timestamp of the frame
        /// </summary>
        public TimeSpan Timestamp { get; set; }

        /// <summary>
        /// Paints the current frame onto the given bitmap
        /// </summary>
        /// <param name="bitmap">The image to paint on</param>
        /// <returns>the resulting bitmap, which is the same as what you passed in as long as it was not null</returns>
        public abstract ConsoleBitmap Paint(ref ConsoleBitmap bitmap);
    }

    /// <summary>
    /// A raw frame that contains all of the bitmap data needed to construct a frame
    /// </summary>
    public class ConsoleBitmapRawFrame : ConsoleBitmapFrame
    {
        /// <summary>
        /// The pixel data for the current frame
        /// </summary>
        public ConsoleCharacter[][] Pixels { get; set; }

        /// <summary>
        /// Paints the entire frame onto the given bitmap.  If the given bitmap is null then
        /// a new bitmap of the correct size will be created and assigned to the reference you
        /// have provided.  The normal usage pattern is to pass null when reading the first frame,
        /// which will always be a raw frame.  You can then pass this same bitmap to subsequent calls
        /// to Paint, and it will work whether the subsequent frames are raw frames or diff frames.
        /// 
        /// </summary>
        /// <param name="bitmap">The bitmap to paint on or null to create a new bitmap from the raw frame</param>
        /// <returns>the same bitmap you passed in or one that was created for you</returns>
        public override ConsoleBitmap Paint(ref ConsoleBitmap bitmap)
        {
            bitmap = bitmap ?? new ConsoleBitmap(Pixels.Length, Pixels[0].Length);
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

    /// <summary>
    /// A frame that contains only the pixel data for pixels that have changed since the previous frame
    /// </summary>
    public class ConsoleBitmapDiffFrame : ConsoleBitmapFrame
    {
        /// <summary>
        /// The pixel diff data, one element for each pixel that has changed since the last frame
        /// </summary>
        public List<ConsoleBitmapPixelDiff> Diffs { get; set; }

        /// <summary>
        /// Paints the diff on top of the given image which, unlike with raw frames, cannot be null,
        /// since a diff frame can only be applied to an existing image
        /// </summary>
        /// <param name="bitmap">the image to apply the diff to</param>
        /// <returns>the same image reference you passed in, updated with the diff</returns>
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

    /// <summary>
    /// Represents a changed pixel
    /// </summary>
    public class ConsoleBitmapPixelDiff
    {
        /// <summary>
        /// The x coordinate of the pixel
        /// </summary>
        public int X { get; set; }
        /// <summary>
        /// The y coordinate of the pixel
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// The value of the pixel
        /// </summary>
        public ConsoleCharacter Value { get; set; }
    }
}
