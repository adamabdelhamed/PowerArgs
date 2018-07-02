using PowerArgs.Cli;
using System;
using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleGames
{
    public class FramerateControl : StackPanel
    {
        private Label sceneFPSLabel, renderFPSLabel, paintFPSLabel, nowControl;
        private SpacetimePanel scene;
        public FramerateControl(SpacetimePanel scene)
        {
            this.scene = scene;
            this.AutoSize = true;
            nowControl = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            sceneFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            renderFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            paintFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHorizontally();
            AddedToVisualTree.SubscribeForLifetime(SetupPolling, this.LifetimeManager);
        }

        private void SetupPolling()
        {
            Application.LifetimeManager.Manage(Application.SetInterval(() =>
            {
                nowControl.Text = $"{scene.SpaceTime.Now.TotalSeconds}".ToConsoleString();
                sceneFPSLabel.Text = $"TODO scene frames per second".ToConsoleString();
                //sceneFPSLabel.Text = FormatFramerateMessage($"{scene.FPS} scene frames per second", scene.FPS);
                renderFPSLabel.Text = FormatFramerateMessage($"{Application.FPS} render frames per second", Application.FPS);
                paintFPSLabel.Text = FormatFramerateMessage($"{Application.PPS} paint frames per second", Application.PPS);
            }, TimeSpan.FromSeconds(1)));
        }

        private ConsoleString FormatFramerateMessage(string message, int framerate)
        {
            if (framerate > 60)
            {
                return message.ToGreen();
            }
            else if (framerate > 30)
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