using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    public class SpaceTimePanel : ConsolePanel
    {
        public bool HeadlessMode { get; set; }
        public SpaceTime SpaceTime { get; private set; }
        private bool resizedSinceLastRender;
        private Dictionary<SpacialElement, SpacialElementRenderer> renderers;
        private SpacialElementBinder thingBinder;
        private AutoResetEvent resetHandle;
        public RealTimeViewingFunction RealTimeViewing { get; private set; }

        public Event AfterUpdate { get; private set; } = new Event();

        public bool PropagateExceptions { get; set; } = true;

        public SpaceTimePanel(int w, int h, SpaceTime time = null)
        {
            this.Width = w;
            this.Height = h;
            Background = ConsoleColor.White;
            renderers = new Dictionary<SpacialElement, SpacialElementRenderer>();
            thingBinder = new SpacialElementBinder();
            resetHandle = new AutoResetEvent(false);
            this.SpaceTime = time ?? new SpaceTime(w, h, increment: TimeSpan.FromSeconds(.05));
            this.SpaceTime.Invoke(() =>
            {
                RealTimeViewing = new RealTimeViewingFunction(this.SpaceTime) { Enabled = true };
                this.SpaceTime.ChangeTrackingEnabled = true;
                this.SpaceTime.EndOfCycle.SubscribeForLifetime(() => UpdateViewInternal(), this);
             });

            this.AddedToVisualTree.SubscribeForLifetime(() =>
            {
                this.OnDisposed(()=> resetHandle.Set());
            }, this);

            this.SpaceTime.UnhandledException.SubscribeForLifetime((ex) =>
            {
                resetHandle.Set();
                Application?.InvokeNextCycle(() =>
                {
                    if (PropagateExceptions)
                    {
                        throw new AggregateException(ex.Exception);
                    }
                });
            }, this);

            this.SubscribeForLifetime(nameof(Bounds), () => { resizedSinceLastRender = false; }, this);
        }

        private void UpdateViewInternal()
        {
            if(HeadlessMode)
            {
                SpaceTime.ClearChanges();
                return;
            }

            if(SpaceTime.AddedElements.Count == 0 && SpaceTime.ChangedElements.Count == 0 && SpaceTime.RemovedElements.Count == 0)
            {
                return;
            }
            resetHandle.Reset();
            Application.InvokeNextCycle(() =>
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
            AfterUpdate.Fire();
        }

        private bool SizeAndLocate(SpacialElementRenderer r)
        {
            float eW = r.Element.Width;
            float eH = r.Element.Height;

            float eL = r.Element.Left;
            float eT = r.Element.Top;

            if (eW < .5f && eW > 0)
            {
                eL -= .5f;
                eW = 1;
            }

            if (eH < .5f && eH > 0)
            {
                eT -= .5f;
                eH = 1;
            }

            float wPer = eW / SpaceTime.Width;
            float hPer = eH / SpaceTime.Height;

            float xPer = eL / SpaceTime.Width;
            float yPer = eT / SpaceTime.Height;


            int x = Geometry.Round(xPer * (float)Width);
            int y = Geometry.Round(yPer * (float)Height);
            int w = Geometry.Round(wPer * (float)Width);
            int h = Geometry.Round(hPer * (float)Height);



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
        
        public SpacialElementRenderer()
        {

        }
        
        public virtual void OnBind() 
        {
            this.CompositionMode = Element.CompositionMode;
        }
        public virtual void OnRender()
        {
            Background = Element.BackgroundColor;
            this.ZIndex = Element.ZIndex;
            this.CompositionMode = Element.CompositionMode;
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
