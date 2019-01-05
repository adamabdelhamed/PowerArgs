using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    public class FormTests
    {
        public TestContext TestContext { get; set; }
 
        public class TestFormViewModel : ObservableObject
        {
            public string Name { get => Get<string>(); set => Set(value); }
            [FormReadOnly]
            public int Age { get; set; } = 33;
            [FormLabel("The Address")]
            public string Address { get => Get<string>(); set => Set(value); }

            public TestFormViewModel()
            {
                Name = "Adam";
                Address = "Somewhere here";
            }
        }


        [TestMethod]
        public void TestFormsBasic()
        {
            var app = new CliTestHarness(this.TestContext, 80,20);

            app.QueueAction(() =>
            {
                var viewModel = new TestFormViewModel();
                var formOptions = FormOptions.FromObject(viewModel);
                var form = app.LayoutRoot.Add(new Form(formOptions)).Fill();

                form.Descendents.WhereAs<TextBox>().ForEach(t => t.BlinkEnabled = false);

                Assert.AreEqual("Adam", viewModel.Name);

                var currentTime = TimeSpan.FromSeconds(0);
                var delay = TimeSpan.FromSeconds(.2);
                currentTime += delay;

                // Type a couple of keys after a brief delay
                app.SetTimeout(() => { app.SendKey(new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false)); }, currentTime);
                currentTime += delay;
                app.SetTimeout(() => { app.SendKey(new ConsoleKeyInfo('A', ConsoleKey.Spacebar, false, false, false)); }, currentTime);
                currentTime += delay;

                // Make sure we see the view and view model update
                app.SetTimeout(() => 
                {
                    Assert.AreEqual("Adam A", viewModel.Name);
                    Assert.IsNotNull(app.Find("Adam A".ToWhite()));
                }, currentTime);
                currentTime += delay;

                // Next programatically clear the text via the view model and make sure the old value is no longer on the screen
                app.SetTimeout(() => { viewModel.Name = ""; }, currentTime);
                currentTime += delay;
                app.SetTimeout(() =>  { Assert.IsNull(app.Find("Adam A".ToWhite())); }, currentTime);
                currentTime += delay;

                // Next dynamically add a form element
                app.SetTimeout(() =>
                {
                    formOptions.Elements.Add(new FormElement()
                    {
                        Label = "Dynamic element".ToConsoleString(),
                        ValueControl = new TextBox() { BlinkEnabled = false, Value = "Dynamic red value".ToRed() }
                    });
                }, currentTime);
                currentTime += delay;

                // Next dynamically remove a form element
                app.SetTimeout(() => { formOptions.Elements.RemoveAt(formOptions.Elements.Count-1); }, currentTime);
                currentTime += delay;

                app.SetTimeout(() => { app.Stop(); }, currentTime);
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
