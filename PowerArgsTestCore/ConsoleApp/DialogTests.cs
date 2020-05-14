using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class DialogTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ShowMessageBasicString()
        {
            var app = new CliTestHarness(this.TestContext, 80,20, true);

            app.InvokeNextCycle(async () =>
            {
                Promise dialogPromise;

                // show hello world message, wait for a paint, then take a keyframe of the screen, which 
                // should have the dialog shown
                dialogPromise = Dialog.ShowMessage("Hello world");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);

                // simulate an enter keypress, which should clear the dialog
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                await dialogPromise.AsAwaitable();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void ShowYesConfirmation()
        {
            var app = new CliTestHarness(this.TestContext, 80, 20, true);

            app.InvokeNextCycle(async () =>
            {
                Promise dialogPromise;

                dialogPromise = Dialog.ShowYesConfirmation("Yes or no, no will be clicked");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);

                var noRejected = false;
                dialogPromise.Fail((ex) => noRejected = true);

                // simulate an enter keypress, which should clear the dialog, but should not trigger 
                // the promise to resolve since yes was not chosen
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsTrue(dialogPromise.IsFulfilled);
                Assert.IsTrue(noRejected); // the promise should reject on no

                dialogPromise = Dialog.ShowYesConfirmation("Yes or no, yes will be clicked");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);
                // give focus to the yes option
                app.SendKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false));
                await app.PaintAndRecordKeyFrameAsync();

                // dismiss the dialog
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                await dialogPromise.AsAwaitable();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }


        [TestMethod]
        public void ShowTextInput()
        {
            var app = new CliTestHarness(this.TestContext, 80, 20, true);

            app.InvokeNextCycle(async () =>
            {
                Promise<ConsoleString> dialogPromise;
                dialogPromise = Dialog.ShowRichTextInput(new RichTextDialogOptions()
                {
                    Message = "Rich text input prompt text".ToGreen(),
                });
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);
                app.SendKey(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                var stringVal = (await dialogPromise.AsAwaitable()).ToString();
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                Assert.AreEqual("Adam", stringVal);
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }

        [TestMethod]
        public void ShowEnumOptions()
        {
            var app = new CliTestHarness(this.TestContext, 80, 20, true);

            app.InvokeNextCycle(async () =>
            {
                Promise<ConsoleColor?> dialogPromise;
                dialogPromise = Dialog.ShowEnumOptions<ConsoleColor>("Enum option picker".ToGreen());
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogPromise.IsFulfilled);

                for (var i = 0; i < 6; i++)
                {
                    app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.DownArrow, false, false, false));
                    await app.PaintAndRecordKeyFrameAsync();
                }

                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();

                var enumValue = (await dialogPromise.AsAwaitable());
                Assert.AreEqual(ConsoleColor.DarkGreen, enumValue);
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
