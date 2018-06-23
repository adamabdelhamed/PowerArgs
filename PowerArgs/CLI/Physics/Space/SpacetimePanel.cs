using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class SpacetimePanel : ConsolePanel
    {
        public SpaceTime SpaceTime { get; private set; }

        private bool resizedSinceLastRender;
        private Dictionary<SpacialElement, SpacialElementRenderer> renderers;
        private SpacialElementBinder thingBinder;

        public RealTimeViewingFunction RealTimeViewing { get; private set; }

        public SpacetimePanel(int w, int h, SpaceTime time = null)
        {
            this.Width = w;
            this.Height = h;
            Background = ConsoleColor.White;
            renderers = new Dictionary<SpacialElement, SpacialElementRenderer>();
            thingBinder = new SpacialElementBinder();

            this.SpaceTime = time ?? new SpaceTime(w, h, increment: TimeSpan.FromSeconds(.05));
            this.SpaceTime.QueueAction(() =>
            {
                RealTimeViewing = new RealTimeViewingFunction(this.SpaceTime) { Enabled = true };
                this.SpaceTime.ChangeTrackingEnabled = true;
                this.SpaceTime.AfterTick.SubscribeForLifetime(() => UpdateView(false), this.LifetimeManager);
            });

            this.AddedToVisualTree.SubscribeForLifetime(() =>
            {
                LifetimeManager.Manage(Application.SetInterval(() =>
                {
                    RealTimeViewing?.Evaluate();
                }, TimeSpan.FromSeconds(.1)));
            }, this.LifetimeManager);

            this.SpaceTime.UnhandledException.SubscribeForLifetime((ex) =>
            {
                Application?.QueueAction(() =>
                {
                    throw new AggregateException(ex);
                });
            }, this.LifetimeManager);

            this.SubscribeForLifetime(nameof(Bounds), () => { resizedSinceLastRender = false; }, this.LifetimeManager);
        }

        public void UpdateView(bool force)
        {
            UpdateViewInternal(force);
        }

        private void UpdateViewInternal(bool force)
        {
            foreach (var element in SpaceTime.AddedElements)
            {
                Application.QueueAction(() =>
                {
                    var renderer = thingBinder.Bind(element, SpaceTime);
                    renderers.Add(element, renderer);
                    this.Controls.Add(renderer);
                    SizeAndLocate(renderer);
                    renderer.OnRender();
                });
            }

            foreach (var t in SpaceTime.ChangedElements)
            {
                Application.QueueAction(() =>
                {
                    var renderer = renderers[t];
                    SizeAndLocate(renderer);
                    renderer.OnRender();
                });
            }

            foreach (var t in SpaceTime.RemovedElements)
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
            SpaceTime.ClearChanges();
            Application.Paint();
        }

        private bool SizeAndLocate(SpacialElementRenderer r)
        {
            float wPer = r.Element.Width / SpaceTime.Width;
            float hPer = r.Element.Height / SpaceTime.Height;

            float xPer = r.Element.Left / SpaceTime.Width;
            float yPer = r.Element.Top / SpaceTime.Height;

            int x = (int)Math.Round(xPer * (float)Width);
            int y = (int)Math.Round(yPer * (float)Height);
            int w = (int)Math.Round(wPer * (float)Width);
            int h = (int)Math.Round(hPer * (float)Height);

            if (w == 0 && r.Element.Width > 0)
            {
                w = 1;
            }

            if (h == 0 && r.Element.Height > 0)
            {
                h = 1;
            }

            r.ZIndex = r.Element.ZIndex;

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

    public class SpacialElementBindingAttribute : Attribute
    {
        public Type ThingType { get; set; }
        public SpacialElementBindingAttribute(Type t) { this.ThingType = t; }
    }

    public class SpacialElementBinder
    {
        Dictionary<Type, Type> Bindings;

        public SpacialElementBinder()
        {
            Bindings = LoadBindings();
        }

        public SpacialElementRenderer Bind(SpacialElement t, SpaceTime spaceTime)
        {
            if (t.Renderer != null)
            {
                t.Renderer.Element = t;
                t.Renderer.OnBind();
                return t.Renderer;
            }

            Type binding;
            if (Bindings.TryGetValue(t.GetType(), out binding) == false)
            {
                binding = typeof(SpacialElementRenderer);
            }

            SpacialElementRenderer ret = Activator.CreateInstance(binding) as SpacialElementRenderer;
            ret.Element = t;
            ret.Spacetime = spaceTime;
            ret.OnBind();
            return ret;
        }

        private Dictionary<Type, Type> LoadBindings()
        {
            Dictionary<Type, Type> ret = new Dictionary<Type, Type>();
            List<Type> rendererTypes = new List<Type>();

            foreach (var rendererAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in from type in rendererAssembly.ExportedTypes where type.GetTypeInfo().IsSubclassOf(typeof(SpacialElementRenderer)) select type)
                {
                    if (t.GetTypeInfo().GetCustomAttributes(typeof(SpacialElementBindingAttribute), true).Count() != 1) continue;
                    rendererTypes.Add(t);
                }
            }

            foreach (var thingAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in from type in thingAssembly.ExportedTypes where type.GetTypeInfo().IsSubclassOf(typeof(SpacialElement)) select type)
                {
                    ret.Add(t, FindMatchingBinder(rendererTypes, t));
                }
            }

            return ret;
        }

        private Type FindMatchingBinder(List<Type> rendererTypes, Type thingType)
        {
            var match = (from renderer in rendererTypes where (renderer.GetTypeInfo().GetCustomAttributes(typeof(SpacialElementBindingAttribute), true).First() as SpacialElementBindingAttribute).ThingType == thingType select renderer).SingleOrDefault();
            if (match == null && thingType.GetTypeInfo().BaseType.GetTypeInfo().IsSubclassOf(typeof(SpacialElement)))
            {
                return FindMatchingBinder(rendererTypes, thingType.GetTypeInfo().BaseType);
            }
            else if (match != null)
            {
                return match;
            }
            else
            {
                return typeof(SpacialElementRenderer);
            }
        }
    }

    public class SpacialElementRenderer : ConsoleControl
    {
        public SpacialElement Element { get; set; }
        public SpaceTime Spacetime { get; set; }
        public virtual void OnBind() { }
        public virtual void OnRender() { }

        public SpacialElementRenderer()
        {
            Background = ConsoleColor.Red;
        }
    }
}
