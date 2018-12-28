using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using PowerArgs.Cli;
using PowerArgs;

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
    }
}
