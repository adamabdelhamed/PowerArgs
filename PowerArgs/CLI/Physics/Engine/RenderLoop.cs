using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class RenderLoop
    {
        [ThreadStatic]
        private static RenderLoop _current;

        public static RenderLoop Current
        {
            get
            {
                return _current;
            }
            set
            {
                if(Current != null)
                {
                    throw new InvalidOperationException("There is already a render loop on this thread");
                }
                _current = value;
            }
        }

        private bool paused;
        private DateTime last = DateTime.MinValue;

        private Queue<Interaction> interactionQueue = new Queue<Interaction>();

        public event Func<Exception, bool> ExceptionOccurred;
        public object RenderLoopSync { get; private set; }
        public Realm Realm { get; set; }
        public float SpeedFactor { get; set; }
        public Action Render { get; set; }
        public Dictionary<Thing, ThingRenderer> Renderers { get; private set; }
        public ThingBinder Binder { get; set; }
        public bool RenderEveryFrame { get; set; }

        public TimeSpan _minTimeBetweenRenderIterations;
        public int MaxFPS
        {
            get
            {
                if (_minTimeBetweenRenderIterations == TimeSpan.Zero) return int.MaxValue;
                else return (int)(1000 / _minTimeBetweenRenderIterations.TotalMilliseconds);
            }
            set
            {
                _minTimeBetweenRenderIterations = TimeSpan.FromSeconds(1.0f / value);
            }
        }

        public RenderLoop(float w = 19.2f, float h = 10.8f)
        {
            Realm = new Realm(0, 0, w, h);
            Binder = new ThingBinder();
            RenderLoopSync = new object();
            Renderers = new Dictionary<Thing, ThingRenderer>();
            SpeedFactor = 1;
        }

        public void Pause()
        {
            paused = true;
        }

        public void Resume()
        {
            last = DateTime.MinValue;
            paused = false;
            Start();
        }

        public void QueueInteraction(Interaction interaction)
        {
            lock(interactionQueue)
            {
                interactionQueue.Enqueue(interaction);
            }
        }

        public void QueueAction(Action a)
        {
            QueueInteraction(new OneTimeInteraction(a));
        }

        public void Start()
        {
            if (Render == null) throw new InvalidOperationException("You need to set RealmChangedAction");

            new Task(() =>
            {
                Current = this;
                try
                {
                    while (!paused)
                    {
                        lock(interactionQueue)
                        {
                            while(interactionQueue.Count > 0)
                            {
                                Realm.Add(interactionQueue.Dequeue());
                            }
                        }

                        while (DateTime.Now - last < _minTimeBetweenRenderIterations) ;
                        Tick();
                        if (Realm.HasChanged || RenderEveryFrame)
                        {
                            Render();
                            Realm.Added.Clear();
                            Realm.Removed.Clear();
                            Realm.Updated.Clear();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ExceptionOccurred == null) throw;
                    else if (ExceptionOccurred(ex) == false)
                    {
                        return;
                    }
                }
            }).Start();
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            if (last != DateTime.MinValue)
            {
                TimeSpan delta = now - last;
                delta = TimeSpan.FromSeconds(delta.TotalSeconds * SpeedFactor);
                Realm.TickRate = delta;
            }
            else
            {
                Realm.TickRate = TimeSpan.Zero;
            }

            last = now;
            lock (RenderLoopSync)
            {
                Realm.Tick();
            }
        }
    }
}
