using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
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

            app.QueueAction(async () =>
            {
                app.LayoutRoot.Add(new BitmapControl() { Bitmap = bitmap }).Fill();
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(centerX, centerY, 0, centerY / 2);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Red);
                bitmap.DrawLine(centerX, centerY, 0, 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Yellow);
                bitmap.DrawLine(centerX, centerY, centerX / 2, 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Green);
                bitmap.DrawLine(centerX, centerY, centerX, 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Magenta);
                bitmap.DrawLine(centerX, centerY, (int)(bitmap.Width * .75), 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Cyan);
                bitmap.DrawLine(centerX, centerY, bitmap.Width - 1, 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(centerX, centerY, bitmap.Width - 1, centerY / 2);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.White);
                bitmap.DrawLine(centerX, centerY, 0, bitmap.Height / 2);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Blue);
                bitmap.DrawLine(centerX, centerY, bitmap.Width - 1, bitmap.Height / 2);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(centerX, centerY, 0, (int)(bitmap.Height * .75));
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Red);
                bitmap.DrawLine(centerX, centerY, 0, bitmap.Height - 1);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Yellow);
                bitmap.DrawLine(centerX, centerY, centerX / 2, bitmap.Height - 1);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Green);
                bitmap.DrawLine(centerX, centerY, centerX, bitmap.Height - 1);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Magenta);
                bitmap.DrawLine(centerX, centerY, (int)(bitmap.Width * .75), bitmap.Height - 1);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Cyan);
                bitmap.DrawLine(centerX, centerY, bitmap.Width - 1, bitmap.Height - 1);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(centerX, centerY, bitmap.Width - 1, (int)(bitmap.Height * .75));

                await app.Paint().AsAwaitable();
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

            app.QueueAction(async () =>
            {
                app.LayoutRoot.Add(new BitmapControl() { Bitmap = bitmap }).Fill();
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(0, centerY / 2, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Red);
                bitmap.DrawLine(0, 0, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Yellow);
                bitmap.DrawLine(centerX / 2, 0, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Green);
                bitmap.DrawLine(centerX, centerY, centerX, 0);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Magenta);
                bitmap.DrawLine((int)(bitmap.Width * .75), 0, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Cyan);
                bitmap.DrawLine(bitmap.Width - 1, 0, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(bitmap.Width - 1, centerY / 2, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.White);
                bitmap.DrawLine(0, bitmap.Height / 2, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Blue);
                bitmap.DrawLine(bitmap.Width - 1, bitmap.Height / 2, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(0, (int)(bitmap.Height * .75), centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Red);
                bitmap.DrawLine(0, bitmap.Height - 1, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Yellow);
                bitmap.DrawLine(centerX / 2, bitmap.Height - 1, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Green);
                bitmap.DrawLine(centerX, bitmap.Height - 1, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Magenta);
                bitmap.DrawLine((int)(bitmap.Width * .75), bitmap.Height - 1, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Cyan);
                bitmap.DrawLine(bitmap.Width - 1, bitmap.Height - 1, centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();

                bitmap.Pen = new ConsoleCharacter('X', ConsoleColor.Gray);
                bitmap.DrawLine(bitmap.Width - 1, (int)(bitmap.Height * .75), centerX, centerY);
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
