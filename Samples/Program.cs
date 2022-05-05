using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

new MyApp().Run();

public class MyApp : GameApp
{
    protected override async Task Startup()
    {
        InitPause();
        var random = new Random(100);

        var camera = LayoutRoot.Add(new Camera() { BigBounds = new RectF(0, 0, 400, 400) }).Fill();
        camera.CameraLocation = camera.BigBounds.Center.ToRect(camera.Width, camera.Height).TopLeft;

        FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.W, null, () => DefaultColliderGroup.SpeedRatio = DefaultColliderGroup.SpeedRatio + .1f , this);
        FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.S, null, () => DefaultColliderGroup.SpeedRatio = Math.Max(0, DefaultColliderGroup.SpeedRatio - .1f) , this);

        while(true)
        {
            var left = camera.Add(new ConsoleControl() 
            { 
                Width = 5,
                Height = 2, 
                X = ConsoleMath.Round(camera.BigBounds.Center.Left - 50),
                Y = ConsoleMath.Round(camera.BigBounds.Center.Top),
                Background = new RGB((byte)random.Next(60, 120), (byte)random.Next(60, 120), (byte)random.Next(60, 120))
            });

            var right = camera.Add(new ConsoleControl()
            {
                Width = 5,
                Height = 2,
                X = ConsoleMath.Round(camera.BigBounds.Center.Left + 50),
                Y = ConsoleMath.Round(camera.BigBounds.Center.Top),
                Background = new RGB((byte)random.Next(60, 120), (byte)random.Next(60, 120), (byte)random.Next(60, 120))
            });

            await Task.WhenAll(left.FadeIn(delayProvider: DelayProvider), right.FadeIn(delayProvider: DelayProvider));

            var leftV = new Velocity2(left, DefaultColliderGroup) { Bounce = true };
            leftV.Speed = 90;
            leftV.Angle = Angle.Right;

            var rightV = new Velocity2(right, DefaultColliderGroup) { Bounce = true };
            rightV.Speed = 10;
            rightV.Angle = Angle.Left;

            FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, () =>
            {
                leftV.SpeedRatio = leftV.SpeedRatio + .1f;
            }, this);

            FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, () =>
            {
                leftV.SpeedRatio = Math.Max(0, leftV.SpeedRatio - .1f);
            }, this);

            await TaskEx.WhenAny(PauseManager.Delay(5000), leftV.ImpactOccurred.CreateNextFireTask());
            await Task.WhenAll(left.FadeOut(duration: 2000, delayProvider: DelayProvider), right.FadeOut(duration: 2000, delayProvider: DelayProvider));
            left.Dispose();
            right.Dispose();
        }
        camera.BigBounds = default;
    }

    private void InitPause()
    {
        PauseManager.OnPaused.SubscribeForLifetime(async lt =>
        {
            await Dialog.ShowMessage(new DialogButtonOptions()
            {
                Message = "Paused".ToCyan(),
                Options = new List<DialogOption>()
                {
                    new DialogOption(){ DisplayText = "Resume".ToYellow() }
                },
            });
            PauseManager.State = PauseManager.PauseState.Running;
        }, this);
        FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.P, null, () => PauseManager.State = PauseManager.PauseState.Paused, this);
    }
}

public class GameApp : ConsoleApp
{
    public IDelayProvider DelayProvider => PauseManager.DelayProvider;
    public PauseManager PauseManager { get; private set; }
    public ColliderGroup DefaultColliderGroup { get; private set; }
    public static GameApp Current => ConsoleApp.Current as GameApp; 

    protected override Task Startup()
    {
        PauseManager = new PauseManager();
        DefaultColliderGroup = new ColliderGroup(this) { PauseManager = PauseManager };
        return Task.CompletedTask;
    }
}

