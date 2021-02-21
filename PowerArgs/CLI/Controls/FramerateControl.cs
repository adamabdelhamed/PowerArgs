using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace PowerArgs.Cli
{
    public class FramerateControl : StackPanel
    {
        private Label renderFPSLabel, paintFPSLabel, nowControl, sleepTimeLabel,zeroSpinsLabel, nonZeroSpinsLabel, elementsControl, functionsControl;
        private SpaceTimePanel scene;
        public FramerateControl(SpaceTimePanel scene)
        {
            this.scene = scene;
            this.AutoSize = true;
            nowControl = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            elementsControl = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            functionsControl = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            renderFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            paintFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            sleepTimeLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            zeroSpinsLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            nonZeroSpinsLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            AddedToVisualTree.SubscribeForLifetime(SetupPolling, this);
        }

        private void SetupPolling()
        {
            Application.OnDisposed(Application.SetInterval(() =>
            {
                if (Application == null)
                {
                    return;
                }
                nowControl.Text = $"Now: {scene.SpaceTime.Now.TotalSeconds}".ToConsoleString();
                //sceneFPSLabel.Text = FormatFramerateMessage($"{scene.FPS} scene frames per second", scene.FPS);
                renderFPSLabel.Text = FormatFramerateMessage($"{Application.CyclesPerSecond} UI cycles per second", Application.CyclesPerSecond, true);
                paintFPSLabel.Text = FormatFramerateMessage($"{Application.PaintRequestsProcessedPerSecond} paint frames per second", Application.PaintRequestsProcessedPerSecond, false);
                sleepTimeLabel.Text = (scene.RealTimeViewing.SleepSummary).ToConsoleString();
                zeroSpinsLabel.Text = (scene.RealTimeViewing.ZeroSleepCycles + " zero spin cycles").ToConsoleString();
                nonZeroSpinsLabel.Text = (scene.RealTimeViewing.SleepCycles + " non-zero spin cycles").ToConsoleString();
                scene.SpaceTime.InvokeNextCycle(() =>
                {
                    var functionCount = Time.CurrentTime.Functions.Count();
                    var elementCount = SpaceTime.CurrentSpaceTime.Elements.Count();

                    Application?.InvokeNextCycle(() =>
                    {
                        elementsControl.Text = $"SpacialElements: {elementCount}".ToConsoleString();
                        functionsControl.Text = $"Time Functions: {functionCount}".ToConsoleString();
                    });
                });


            }, TimeSpan.FromSeconds(1)));
        }

 
        private ConsoleString FormatFramerateMessage(string message, int framerate, bool style)
        {
            if(!style)
            {
                return message.ToConsoleString();
            }
            else if (framerate > 25)
            {
                return message.ToGreen();
            }
            else if (framerate > 10)
            {
                return message.ToYellow();
            }
            else
            {
                return message.ToRed();
            }
        }

    }
}