using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace Benchmarks;

public class HitDetectionBenchmark : UIBenchmark
{
    protected override float RunActual(ConsoleApp app)
    {
        int n = 0;
        app.Invoke(async () =>
        {
            var stp = app.LayoutRoot.Add(new SpaceTimePanel(new SpaceTime(app.LayoutRoot.Width, app.LayoutRoot.Height))).CenterBoth();
            app.LayoutRoot.Background = RGB.Black;
            stp.Background = new RGB(20, 20, 20);
            stp.SpaceTime.Start();
            var random = new Random(100);
            var nLabel = app.LayoutRoot.Add(new Label() { Text = $"N = {n}".ToCyan() }).CenterHorizontally().DockToTop(padding: 2);
            RealTimeState latestState = RealTimeState.Cold;
            stp.SpaceTime.Invoke(async () =>
            {
                stp.RealTimeViewing.RealTimeStateChanged.SubscribeForLifetime(state =>
                {
                    latestState = state;
                    if(state == RealTimeState.Hot)
                    {
                        app.Invoke(() =>
                        {
                            stp.SpaceTime.Stop();
                            app.Stop();
                        });
                    }
                }, stp.SpaceTime);
                var camBounds = new RectF(0, 0, 200, 200);
                stp.CameraTopLeft = camBounds.Center.ToRect(stp.Width, stp.Height).TopLeft;
                var left = stp.SpaceTime.Add(new SpacialElement(2, camBounds.Height, 0, 0));
                var right = stp.SpaceTime.Add(new SpacialElement(2, camBounds.Height, camBounds.Width, 0));
                var top = stp.SpaceTime.Add(new SpacialElement(camBounds.Width, 1, 0, 0));
                var bottom = stp.SpaceTime.Add(new SpacialElement(camBounds.Width, 1, 0, camBounds.Height));

                while (true)
                {
                    var el = stp.SpaceTime.Add(new SpacialElement(2, 1, (camBounds.Width - 2)/2, (camBounds.Height - 1) / 2));
                    el.BackgroundColor = new RGB((byte)random.Next(60,120), (byte)random.Next(60, 120), (byte)random.Next(60, 120));
                    while(stp.SpaceTime.Elements.Where(e => e != el && e.Touches(el)).Any())
                    {
                        el.MoveTo(random.Next(0, (int)stp.SpaceTime.Width - 2), random.Next(0, (int)stp.SpaceTime.Height - 1));
                    }
                    var v = new Velocity(el) { Bounce = true };
                    v.Speed = 80;
                    v.Angle = random.Next(0, 360);
                    n++;
                    app.Invoke(() => nLabel.Text = $"N = {n}".ToConsoleString(latestState == RealTimeState.Cold ? RGB.Cyan : latestState == RealTimeState.Warm ? RGB.Yellow : RGB.Red));
                    await Task.Delay(10);
                }
            });
        });
        app.Run();
        return n;
    }
}

