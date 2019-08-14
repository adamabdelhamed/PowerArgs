using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    public class SpacetimePanel : ConsolePanel
    {
        public SpaceTime SpaceTime { get; private set; }
        private bool resizedSinceLastRender;
        private Dictionary<SpacialElement, SpacialElementRenderer> renderers;
        private SpacialElementBinder thingBinder;
        private AutoResetEvent resetHandle;
        public RealTimeViewingFunction RealTimeViewing { get; private set; }

        public SpacetimePanel(int w, int h, SpaceTime time = null)
        {
            this.Width = w;
            this.Height = h;
            Background = ConsoleColor.White;
            renderers = new Dictionary<SpacialElement, SpacialElementRenderer>();
            thingBinder = new SpacialElementBinder();
            resetHandle = new AutoResetEvent(false);
            this.SpaceTime = time ?? new SpaceTime(w, h, increment: TimeSpan.FromSeconds(.05));
            this.SpaceTime.QueueAction(() =>
            {
                RealTimeViewing = new RealTimeViewingFunction(this.SpaceTime) { Enabled = true };
                this.SpaceTime.ChangeTrackingEnabled = true;
                this.SpaceTime.AfterTick.SubscribeForLifetime(() => UpdateViewInternal(), this);
            });

            this.AddedToVisualTree.SubscribeForLifetime(() =>
            {
                this.SpaceTime.Application = this.Application;
                this.OnDisposed(Application.SetInterval(() =>
                {
                    RealTimeViewing?.Evaluate();
                }, TimeSpan.FromSeconds(.1)));
                this.OnDisposed(()=> resetHandle.Set());
            }, this);

            this.SpaceTime.UnhandledException.SubscribeForLifetime((ex) =>
            {
                resetHandle.Set();
                Application?.QueueAction(() =>
                {
                    throw new AggregateException(ex);
                });
            }, this);

            this.SubscribeForLifetime(nameof(Bounds), () => { resizedSinceLastRender = false; }, this);
        }

        private void UpdateViewInternal()
        {
            if(SpaceTime.AddedElements.Count == 0 && SpaceTime.ChangedElements.Count == 0 && SpaceTime.RemovedElements.Count == 0)
            {
                return;
            }

            Application.QueueAction(() =>
            {
                foreach (var e in SpaceTime.AddedElements)
                {
                    var renderer = thingBinder.Bind(e, SpaceTime);
                    renderers.Add(e, renderer);
                    this.Controls.Add(renderer);
                    SizeAndLocate(renderer);
                    renderer.OnRender();
                }

                foreach (var e in SpaceTime.ChangedElements)
                {
                    SizeAndLocate(renderers[e]);
                    renderers[e].OnRender();
                }

                foreach (var e in SpaceTime.RemovedElements)
                {
                    var renderer = renderers[e];
                    renderers.Remove(e);
                    Controls.Remove(renderer);
                }

                if (resizedSinceLastRender)
                {
                    foreach (var r in renderers.Values)
                    {
                        SizeAndLocate(r);
                    }
                }
                resetHandle.Set();
            });

            resetHandle.WaitOne();
            resizedSinceLastRender = false;
            SpaceTime.ClearChanges();
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
                if(rendererAssembly.IsDynamic)
                {
                    continue;
                }

                var types = rendererAssembly.ExportedTypes.ToList();
                foreach (Type t in from type in types where IsRendererType(type) select type)
                {
                    if (t.GetTypeInfo().GetCustomAttributes(typeof(SpacialElementBindingAttribute), true).Count() != 1) continue;
                    rendererTypes.Add(t);
                }
            }

            foreach (var thingAssembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.IsDynamic == false))
            {
                foreach (Type t in from type in thingAssembly.ExportedTypes where type.GetTypeInfo().IsSubclassOf(typeof(SpacialElement)) select type)
                {
                    ret.Add(t, FindMatchingBinder(rendererTypes, t));
                }
            }

            return ret;
        }

        private bool IsRendererType(Type type)
        {
            return type.GetTypeInfo().IsSubclassOf(typeof(SpacialElementRenderer));
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

    public class SpacialElementRenderer : ConsolePanel
    {
        public SpacialElement Element { get; set; }
        public SpaceTime Spacetime { get; set; }
        public virtual void OnBind() { }
        public virtual void OnRender()
        {
            Background = Element.BackgroundColor;
            this.ZIndex = Element.ZIndex;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if(Element.Pen.HasValue)
            {
                context.FillRect(Element.Pen.Value, 0, 0, Width, Height);
            }
            else
            {
                base.OnPaint(context);
            }
        }
    }
}
