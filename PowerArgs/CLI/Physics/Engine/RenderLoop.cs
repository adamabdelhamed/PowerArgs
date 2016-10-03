using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class RenderLoop : IObservableObject
    {
        private ObservableObject observable = new ObservableObject();

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

        private bool stopRequested;
        private DateTime last = DateTime.MinValue;

        private Queue<Interaction> interactionQueue = new Queue<Interaction>();

        public event Func<Exception, bool> ExceptionOccurred;
        public object RenderLoopSync { get; private set; }
        public Realm Realm { get; set; }
        public float SpeedFactor { get { return observable.Get<float>(); } set { observable.Set(value); } }
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

        public bool SuppressEqualChanges
        {
            get
            {
                return observable.SuppressEqualChanges;
            }

            set
            {
                observable.SuppressEqualChanges = true;
            }
        }

        public RenderLoop(float w = 19.2f, float h = 10.8f)
        {
            Realm = new Realm(0, 0, w, h) { RenderLoop = this};
            Binder = new ThingBinder();
            RenderLoopSync = new object();
            Renderers = new Dictionary<Thing, ThingRenderer>();
            SpeedFactor = 1;
        }

        public void Stop()
        {
            QueueAction(() => { stopRequested = true; });
        }

        public void Resume()
        {
            last = DateTime.MinValue;
            stopRequested = false;
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

        public Task Start()
        {
            if (Render == null) throw new InvalidOperationException("You need to set RealmChangedAction");

            return Task.Factory.StartNew(() =>
            {
                stopRequested = false;
                Current = this;
                try
                {
                    while (!stopRequested)
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
            });
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

        public PropertyChangedSubscription SubscribeUnmanaged(string propertyName, Action handler)
        {
            return observable.SubscribeUnmanaged(propertyName, handler);
        }

        public void SubscribeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        }

        public PropertyChangedSubscription SynchronizeUnmanaged(string propertyName, Action handler)
        {
            return observable.SynchronizeUnmanaged(propertyName, handler);
        }

        public void SynchronizeForLifetime(string propertyName, Action handler, LifetimeManager lifetimeManager)
        {
            observable.SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        }
    }
}
