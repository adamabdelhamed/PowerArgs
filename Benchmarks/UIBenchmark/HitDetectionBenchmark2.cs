using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace Benchmarks;

public class HitDetectionBenchmark2 : UIBenchmark
{
    protected override float RunActual(ConsoleApp app)
    {
        int n = 0;
        app.Invoke(async () =>
        {
            await Task.Delay(1000);
            app.LayoutRoot.Background = new RGB(20, 20, 20);
            var random = new Random(100);
            var nLabel = app.LayoutRoot.Add(new Label() { ZIndex = 100, Text = $"N = {n}".ToCyan() }).CenterHorizontally().DockToTop(padding: 2);

            var leftWall = app.LayoutRoot.Add(new ConsoleControl());
            var rightWall = app.LayoutRoot.Add(new ConsoleControl());
            var topWall = app.LayoutRoot.Add(new ConsoleControl());
            var bottomWall = app.LayoutRoot.Add(new ConsoleControl());

            var w = 8000;
            var h = 8000;
            leftWall.Bounds = app.LayoutRoot.Center().Offset(-w/2, 0).ToRect(2, h);
            rightWall.Bounds = app.LayoutRoot.Center().Offset(w/2, 0).ToRect(2, h);
            topWall.Bounds = app.LayoutRoot.Center().Offset(0, -h/2).ToRect(w, 1);
            bottomWall.Bounds = app.LayoutRoot.Center().Offset(0, h/2).ToRect(w, 1);

            var colliderGroup = new ColliderGroup(app);
            new Velocity2(leftWall, colliderGroup);
            new Velocity2(rightWall, colliderGroup);
            new Velocity2(topWall, colliderGroup);
            new Velocity2(bottomWall, colliderGroup);

            var slowCount = 0;
            while (true)
            {
                slowCount = colliderGroup.LatestDT < 50 ? 0 : slowCount + 1;
                if (slowCount == 10) break;

                var el = app.LayoutRoot.Add(new ConsoleControl());
                el.Bounds = app.LayoutRoot.Bounds.Center.ToRect(2, 1);
                el.Background = new RGB((byte)random.Next(60,120), (byte)random.Next(60, 120), (byte)random.Next(60, 120));
                while(app.LayoutRoot.Controls.Where(e => e != el && e.Touches(el)).Any())
                {
                    el.Bounds = new RectF(random.Next(0, app.LayoutRoot.Width - 2), random.Next(0, app.LayoutRoot.Height - 1), el.Width, el.Height);
                }
                var v = new Velocity2(el, colliderGroup) { Bounce = true };
                v.Speed = random.Next(0,80);
                v.Angle = random.Next(0, 360);
                n++;
                nLabel.Text = $"N = {n}, DT = {ConsoleMath.Round(colliderGroup.LatestDT)}".ToConsoleString();
                await Task.Delay(10);
            }
            app.Stop();
        });
        app.Run();
        return n;
    }
}

public class HitDetectionBenchmark3 : UIBenchmark
{
    protected override float RunActual(ConsoleApp app)
    {
        int n = 0;
        app.Invoke(async () =>
        {
            await Task.Delay(1000);
            app.LayoutRoot.Background = new RGB(20, 20, 20);
            var random = new Random(100);
            var nLabel = app.LayoutRoot.Add(new Label() { ZIndex = 100, Text = $"N = {n}".ToCyan() }).CenterHorizontally().DockToTop(padding: 2);

            var leftWall = app.LayoutRoot.Add(new ConsoleControl());
            var rightWall = app.LayoutRoot.Add(new ConsoleControl());
            var topWall = app.LayoutRoot.Add(new ConsoleControl());
            var bottomWall = app.LayoutRoot.Add(new ConsoleControl());

            var w = 8000;
            var h = 8000;
            leftWall.Bounds = app.LayoutRoot.Center().Offset(-w / 2, 0).ToRect(2, h);
            rightWall.Bounds = app.LayoutRoot.Center().Offset(w / 2, 0).ToRect(2, h);
            topWall.Bounds = app.LayoutRoot.Center().Offset(0, -h / 2).ToRect(w, 1);
            bottomWall.Bounds = app.LayoutRoot.Center().Offset(0, h / 2).ToRect(w, 1);

            var colliderGroup = new ColliderGroup(app);
            new Velocity2(leftWall, colliderGroup);
            new Velocity2(rightWall, colliderGroup);
            new Velocity2(topWall, colliderGroup);
            new Velocity2(bottomWall, colliderGroup);

            var slowCount = 0;
            while (true)
            {
                slowCount = colliderGroup.LatestDT < 50 ? 0 : slowCount + 1;
                if (slowCount == 10) break;

                var el = app.LayoutRoot.Add(new ConsoleControl());
                el.Bounds = app.LayoutRoot.Bounds.Center.ToRect(2, 1);
                el.Background = new RGB((byte)random.Next(60, 120), (byte)random.Next(60, 120), (byte)random.Next(60, 120));
                while (app.LayoutRoot.Controls.Where(e => e != el && e.Touches(el)).Any())
                {
                    el.Bounds = new RectF(random.Next(0, app.LayoutRoot.Width - 2), random.Next(0, app.LayoutRoot.Height - 1), el.Width, el.Height);
                }
                var v = new Velocity2(el, colliderGroup) { Bounce = true };
                v.Speed = 60;
                v.Angle = random.Next(0, 360);
                n++;

                if(n % 100 == 0)
                {
                    while(app.LayoutRoot.Controls.Count > 500)
                    {
                        app.LayoutRoot.Controls[1].Dispose();
                    }
                }

                nLabel.Text = $"N = {n}, DT = {ConsoleMath.Round(colliderGroup.LatestDT)}".ToConsoleString();
                await Task.Delay(50);
            }
            app.Stop();
        });
        app.Run();
        return n;
    }
}