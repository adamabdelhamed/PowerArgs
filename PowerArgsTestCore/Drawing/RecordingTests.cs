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
    [TestCategory(Categories.Drawing)]
    public class RecordingTests
    {
        public TestContext TestContext { get; set; }

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

                InMemoryConsoleBitmapFrame frame;
                while((lastFrameIndex =  video.Seek(destination, out frame, lastFrameIndex >= 0 ? lastFrameIndex : 0)) != numFrames - 1)
                {
                    destination = destination.Add(TimeSpan.FromMilliseconds(1));
                }
                sw.Stop();
                Assert.IsTrue(sw.ElapsedMilliseconds < 10);
                Console.WriteLine($"Playback took {sw.ElapsedMilliseconds} ms");
            }
        }

        [TestMethod]
        public void TestPlaybackEndToEnd()
        {
            int w = 10, h = 1;
            var temp = Path.GetTempFileName();
            using (var stream = File.OpenWrite(temp))
            {
                var writer = new ConsoleBitmapStreamWriter(stream) {  CloseInnerStream = false};
                var bitmap = new ConsoleBitmap(w, h);

                for(var i = 0; i < bitmap.Width; i++)
                {
                    bitmap.Pen = new ConsoleCharacter(' ');
                    bitmap.FillRect(0, 0, bitmap.Width, bitmap.Height);
                    bitmap.Pen = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Red);
                    bitmap.DrawPoint(i, 0);
                    writer.WriteFrame(bitmap, true, TimeSpan.FromSeconds(.5*i));
                }
                writer.Dispose();
            }

            var app = new CliTestHarness(this.TestContext, 80, 30);

            app.InvokeNextCycle(() =>
            {
                var player = app.LayoutRoot.Add(new ConsoleBitmapPlayer()).Fill();
                player.Load(File.OpenRead(temp));
                app.SetTimeout(() => app.SendKey(new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false)), TimeSpan.FromMilliseconds(100));
                var playStarted = false;
                player.SubscribeForLifetime(nameof(player.State), () =>
                {
                    if(player.State == PlayerState.Playing)
                    {
                        playStarted = true;
                    }
                    else if(player.State == PlayerState.Stopped && playStarted)
                    {
                        app.Stop();
                    }
                }, app);
            });

            app.Start().Wait();
            Thread.Sleep(100);
            app.AssertThisTestMatchesLKGFirstAndLastFrame();
        }
    }
}
