using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using System.Diagnostics;

namespace ArgsTests.CLI.Recording
{
    [TestClass]
    public class RecordingTests
    {
        [TestMethod]
        public void TestRecordVideoBasic()
        {
            ConsoleBitmap bitmap = new ConsoleBitmap(4, 2), redBitmap = null, greenBitmap = null, magentaPixelBitmap = null;
            using (var sharedStream = new MemoryStream())
            {
                var bitmapVideoWriter = new ConsoleBitmapStreamWriter(sharedStream) { CloseInnerStream = false };
                
                bitmap = new ConsoleBitmap(4, 2);
                redBitmap = bitmapVideoWriter.WriteFrame(bitmap.FillRect(ConsoleCharacter.RedBG())).Clone();
                greenBitmap = bitmapVideoWriter.WriteFrame(bitmap.FillRect(ConsoleCharacter.GreenBG())).Clone();
                magentaPixelBitmap = bitmapVideoWriter.WriteFrame(bitmap.DrawPoint(ConsoleCharacter.MagentaBG(), 0, 0)).Clone();
                bitmapVideoWriter.Dispose();

                sharedStream.Position = 0; // rewind the stream to the beginning to read it back

                // create a reader and make sure we can read each frame back exactly as they were written
                var bitmapVideoReader = new ConsoleBitmapStreamReader(sharedStream);
                Assert.AreEqual(redBitmap, bitmapVideoReader.ReadFrame().CurrentBitmap);
                Assert.AreEqual(greenBitmap, bitmapVideoReader.ReadFrame().CurrentBitmap);
                Assert.AreEqual(magentaPixelBitmap, bitmapVideoReader.ReadFrame().CurrentBitmap);
                Assert.IsNull(bitmapVideoReader.ReadFrame().CurrentFrame);
            }
        }

        /// <summary>
        /// This test verifies that a large video can be read via the seek method quickly as long as the
        /// caller sends back the last frame index when they recall seek. Without this optimization this
        /// test should take a long time to run (almost a full second). With the optimization it should
        /// run in about a millisecond.
        /// </summary>
        [TestMethod, Timeout(1000)]
        public void TestRecordVideoLargeVideo()
        {
            ConsoleBitmap bitmap = new ConsoleBitmap(1, 1);
            var numFrames = 10000;
            using (var sharedStream = new MemoryStream())
            {
                var bitmapVideoWriter = new ConsoleBitmapStreamWriter(sharedStream) { CloseInnerStream = false };

                for (var i = 0; i < numFrames; i++)
                {
                    bitmapVideoWriter.WriteFrame(bitmap, true, TimeSpan.FromMilliseconds(i));
                }
                bitmapVideoWriter.Dispose();

                sharedStream.Position = 0; // rewind the stream to the beginning to read it back

                var destination = TimeSpan.Zero;

                var reader = new ConsoleBitmapStreamReader(sharedStream);
                var video = reader.ReadToEnd();
                var lastFrameIndex = 0;
                var sw = Stopwatch.StartNew();
                while((lastFrameIndex =  video.TrySeek(destination, out bitmap, lastFrameIndex >= 0 ? lastFrameIndex : 0)) != numFrames - 1)
                {
                    destination = destination.Add(TimeSpan.FromMilliseconds(1));
                }
                sw.Stop();
                Assert.IsTrue(sw.ElapsedMilliseconds < 10);
                Console.WriteLine($"Playback took {sw.ElapsedMilliseconds} ms");
            }
        }
    }
}
