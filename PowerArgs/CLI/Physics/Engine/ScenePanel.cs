using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ScenePanel : ConsolePanel
    {
        public Scene Scene { get; private set; }

        private bool resizedSinceLastRender;
        private Dictionary<Thing, ThingRenderer> renderers;
        private ThingBinder thingBinder;
        public Size PixelSize
        {
            get
            {
                return new Physics.Size()
                {
                    H = (1.0f / this.Height) * Scene.Bounds.H,
                    W = (1.0f / this.Width) * Scene.Bounds.W,
                };
            }
        }

        public ScenePanel(int w, int h)
        {
            renderers = new Dictionary<Thing, ThingRenderer>();
            thingBinder = new ThingBinder();
            this.Scene = new Scene(0,0, w, h);
            this.Scene.ExceptionOccurred.SubscribeForLifetime((ex) => 
            {
                Application.QueueAction(() => 
                {
                    throw new Exception("There was an unhandled Physics exception", ex);
                });
            },this.LifetimeManager);
            this.Size = new Cli.Size(w, h);
            Scene.MaxFPS = 100;
            Scene.RenderImpl = UpdateView;
            this.SubscribeForLifetime(nameof(Bounds), ()=> { resizedSinceLastRender = false; }, this.LifetimeManager);
        }

        public void UpdateView()
        {
            UpdateView(false);
        }

        public void UpdateView(bool force)
        {

            UpdateViewInternal(force);
        }

        private void UpdateViewInternal(bool force)
        {

            foreach (Thing t in Scene.AddedSinceLastRender)
            {
                Application.QueueAction(() =>
                {
                    var renderer = thingBinder.Bind(t);
                    renderers.Add(t, renderer);
                    this.Controls.Add(renderer);
                    SizeAndLocate(renderer);
                    renderer.OnRender();
                });
            }

            foreach (Thing t in Scene.UpdatedSinceLastRender)
            {
                Application.QueueAction(() =>
                {
                    var renderer = renderers[t];
                    SizeAndLocate(renderer);
                    renderer.OnRender();
                });
            }

            foreach (Thing t in Scene.RemovedSinceLastRender)
            {
                Application.QueueAction(() =>
                {
                    var renderer = renderers[t];
                    renderers.Remove(t);
                    Controls.Remove(renderer);
                });
            }

            if (force || resizedSinceLastRender)
            {
                foreach (var renderer in renderers.Values)
                {
                    Application.QueueAction(() =>
                    {
                        SizeAndLocate(renderer);
                    });
                }
            }

            resizedSinceLastRender = false;
            Application.Paint();  
        }

        private bool SizeAndLocate(ThingRenderer r)
        {
            float wPer = r.Thing.Bounds.Size.W / (float)Scene.Bounds.Size.W;
            float hPer = r.Thing.Bounds.Size.H / (float)Scene.Bounds.Size.H;

            float xPer = r.Thing.Bounds.Location.X / (float)Scene.Bounds.Size.W;
            float yPer = r.Thing.Bounds.Location.Y / (float)Scene.Bounds.Size.H;

            int x = (int)Math.Round(xPer * (float)Width);
            int y = (int)Math.Round(yPer * (float)Height);
            int w = (int)Math.Round(wPer * (float)Width);
            int h = (int)Math.Round(hPer * (float)Height);

            if(w == 0 && r.Thing.Bounds.W > 0)
            {
                w = 1;
            }

            if (h == 0 && r.Thing.Bounds.H > 0)
            {
                h = 1;
            }

            if (x != r.X || y != r.Y || w != r.Width || h != r.Height)
            {
                r.Width = w;
                r.Height = h;
#if DEBUG
                if (float.IsInfinity(x) || float.IsNaN(x) || x >= -100000 == false && x < 100000 == false) throw new Exception("Out of bounds: " + x + "," + y);
#endif

                r.X = x;
                r.Y = y;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
