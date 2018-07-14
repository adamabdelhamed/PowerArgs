using PowerArgs.Cli;
using System;
using PowerArgs;
using PowerArgs.Cli.Physics;

namespace PowerArgs.Games
{
    public class FramerateControl : StackPanel
    {
        private Label sceneFPSLabel, renderFPSLabel, paintFPSLabel, nowControl, sceneBusyPercentageLabel;
        private SpacetimePanel scene;
        public FramerateControl(SpacetimePanel scene)
        {
            this.scene = scene;
            this.AutoSize = true;
            nowControl = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            sceneFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            renderFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            paintFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            sceneBusyPercentageLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
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
                nowControl.Text = $"{scene.SpaceTime.Now.TotalSeconds}".ToConsoleString();
                sceneFPSLabel.Text = $"TODO scene frames per second".ToConsoleString();
                //sceneFPSLabel.Text = FormatFramerateMessage($"{scene.FPS} scene frames per second", scene.FPS);
                renderFPSLabel.Text = FormatFramerateMessage($"{Application.CyclesPerSecond} UI cycles per second", Application.CyclesPerSecond, true);
                paintFPSLabel.Text = FormatFramerateMessage($"{Application.PaintRequestsProcessedPerSecond} paint frames per second", Application.PaintRequestsProcessedPerSecond, false);
                sceneBusyPercentageLabel.Text = FormatSceneBusyPercentage();
            }, TimeSpan.FromSeconds(1)));
        }

        private ConsoleString FormatSceneBusyPercentage()
        {
            var color = scene.RealTimeViewing.BusyPercentage >= .9 ? ConsoleColor.Red :
                scene.RealTimeViewing.BusyPercentage >= .7 ? ConsoleColor.Yellow :
                ConsoleColor.Green;
            return ("Scene real time budget: "+Math.Round(100 * scene.RealTimeViewing.BusyPercentage) + " %").ToConsoleString(color);
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