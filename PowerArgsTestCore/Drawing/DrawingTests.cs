using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    [TestCategory(Categories.Drawing)]
    public class DrawingTests
    {
        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public void DrawLines()
        {
            var bitmap = new ConsoleBitmap(80, 30);
            var centerX = bitmap.Width / 2;
            var centerY = bitmap.Height / 2;

            var app = new CliTestHarness(TestContext, bitmap.Width, bitmap.Height, true);

            app.InvokeNextCycle(async () =>
            {
                app.LayoutRoot.Add(new BitmapControl() { Bitmap = bitmap }).Fill();
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), centerX, centerY, 0, centerY / 2);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Red), centerX, centerY, 0, 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Yellow), centerX, centerY, centerX / 2, 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Green), centerX, centerY, centerX, 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Magenta), centerX, centerY, (int)(bitmap.Width * .75), 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Cyan), centerX, centerY, bitmap.Width - 1, 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), centerX, centerY, bitmap.Width - 1, centerY / 2);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.White), centerX, centerY, 0, bitmap.Height / 2);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Blue), centerX, centerY, bitmap.Width - 1, bitmap.Height / 2);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), centerX, centerY, 0, (int)(bitmap.Height * .75));
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Red), centerX, centerY, 0, bitmap.Height - 1);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Yellow), centerX, centerY, centerX / 2, bitmap.Height - 1);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Green), centerX, centerY, centerX, bitmap.Height - 1);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Magenta), centerX, centerY, (int)(bitmap.Width * .75), bitmap.Height - 1);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Cyan), centerX, centerY, bitmap.Width - 1, bitmap.Height - 1);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), centerX, centerY, bitmap.Width - 1, (int)(bitmap.Height * .75));

                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void DrawLinesReverse()
        {
            var bitmap = new ConsoleBitmap(80, 30);
            var centerX = bitmap.Width / 2;
            var centerY = bitmap.Height / 2;

            var app = new CliTestHarness(TestContext, bitmap.Width, bitmap.Height, true);

            app.InvokeNextCycle(async () =>
            {
                app.LayoutRoot.Add(new BitmapControl() { Bitmap = bitmap }).Fill();
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), 0, centerY / 2, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Red), 0, 0, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Yellow), centerX / 2, 0, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Green), centerX, centerY, centerX, 0);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Magenta), (int)(bitmap.Width * .75), 0, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Cyan), bitmap.Width - 1, 0, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), bitmap.Width - 1, centerY / 2, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.White), 0, bitmap.Height / 2, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Blue), bitmap.Width - 1, bitmap.Height / 2, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), 0, (int)(bitmap.Height * .75), centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Red), 0, bitmap.Height - 1, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Yellow), centerX / 2, bitmap.Height - 1, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Green), centerX, bitmap.Height - 1, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Magenta), (int)(bitmap.Width * .75), bitmap.Height - 1, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Cyan), bitmap.Width - 1, bitmap.Height - 1, centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();

                bitmap.DrawLine(new ConsoleCharacter('X', ConsoleColor.Gray), bitmap.Width - 1, (int)(bitmap.Height * .75), centerX, centerY);
                await app.RequestPaintAsync();
                app.RecordKeyFrame();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void TestDrawRect()
        {
            var bitmap = new ConsoleBitmap(80, 30);
            var app = new CliTestHarness(TestContext, bitmap.Width, bitmap.Height, true);

            app.InvokeNextCycle(async () =>
            {
                app.LayoutRoot.Add(new BitmapControl() { Bitmap = bitmap }).Fill();
                var pen = new ConsoleCharacter('X', ConsoleColor.Green);
                for (var i = 0; i < 500000; i++)
                {
                    bitmap.DrawRect(pen, 0, 0, bitmap.Width, bitmap.Height);
                }
                await app.PaintAndRecordKeyFrameAsync();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
