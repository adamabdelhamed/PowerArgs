using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Threading.Tasks;

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
                Task dialogTask;

                // show hello world message, wait for a paint, then take a keyframe of the screen, which 
                // should have the dialog shown
                dialogTask = Dialog.ShowMessage("Hello world");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());

                // simulate an enter keypress, which should clear the dialog
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                await dialogTask;
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
                Task dialogTask;

                dialogTask = Dialog.ShowYesConfirmation("Yes or no, no will be clicked");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());

                var noRejected = false;
                dialogTask.Fail((ex) => noRejected = true);

                // simulate an enter keypress, which should clear the dialog, but should not trigger 
                // the Task to resolve since yes was not chosen
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsTrue(dialogTask.IsFulfilled());
                Assert.IsTrue(noRejected); // the Task should reject on no

                dialogTask = Dialog.ShowYesConfirmation("Yes or no, yes will be clicked");
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());
                // give focus to the yes option
                app.SendKey(new ConsoleKeyInfo('\t', ConsoleKey.Tab, true, false, false));
                await app.PaintAndRecordKeyFrameAsync();

                // dismiss the dialog
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                await dialogTask;
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
                Task<ConsoleString> dialogTask;
                dialogTask = Dialog.ShowRichTextInput(new RichTextDialogOptions()
                {
                    Message = "Rich text input prompt text".ToGreen(),
                });
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());
                app.SendKey(new ConsoleKeyInfo('A', ConsoleKey.A, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('d', ConsoleKey.D, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                app.SendKey(new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());
                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                var stringVal = (await dialogTask).ToString();
                await app.RequestPaintAsync();
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
                Task<ConsoleColor?> dialogTask;
                dialogTask = Dialog.ShowEnumOptions<ConsoleColor>("Enum option picker".ToGreen());
                await app.PaintAndRecordKeyFrameAsync();
                Assert.IsFalse(dialogTask.IsFulfilled());

                for (var i = 0; i < 6; i++)
                {
                    app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.DownArrow, false, false, false));
                    await app.PaintAndRecordKeyFrameAsync();
                }

                app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Enter, false, false, false));
                await app.PaintAndRecordKeyFrameAsync();

                var enumValue = (await dialogTask);
                Assert.AreEqual(ConsoleColor.DarkGreen, enumValue);
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
