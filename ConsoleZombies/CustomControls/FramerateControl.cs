using PowerArgs.Cli;
using System;
using PowerArgs;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class FramerateControl : StackPanel
    {
        private Label sceneFPSLabel, renderFPSLabel, paintFPSLabel;
        private Scene scene;
        public FramerateControl(Scene scene)
        {
            this.scene = scene;
            this.AutoSize = true;
            sceneFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHoriontally();
            renderFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHoriontally();
            paintFPSLabel = Add(new Label() { Text = "".ToConsoleString() }).FillHoriontally();
            AddedToVisualTree.SubscribeForLifetime(SetupPolling, this.LifetimeManager);
        }

        private void SetupPolling()
        {
            Application.LifetimeManager.Manage(Application.SetInterval(() =>
            {
                sceneFPSLabel.Text = FormatFramerateMessage($"{scene.FPS} scene frames per second", scene.FPS);
                renderFPSLabel.Text = FormatFramerateMessage($"{Application.FPS} render frames per second", Application.FPS);
                paintFPSLabel.Text = FormatFramerateMessage($"{Application.PPS} paint frames per second", Application.PPS);
            }, TimeSpan.FromSeconds(1)));
        }

        private ConsoleString FormatFramerateMessage(string message, int framerate)
        {
            if(framerate > 60)
            {
                return message.ToGreen();
            }
            else if(framerate > 30)
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
