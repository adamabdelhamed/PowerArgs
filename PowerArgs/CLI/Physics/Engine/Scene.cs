using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class Scene
    {
        [ThreadStatic]
        private static Scene _current;
        public static Scene Current
        {
            get
            {
                return _current;
            }
            private set
            {
                if (Current != null && value != null)
                {
                    throw new InvalidOperationException("There is already a scene on this thread");
                }
                _current = value;
            }
        }

        // privates
        private List<Thing> _things;
        private List<Interaction> _interactions;
        private long NextId;
        private TimeSpan _minTimeBetweenRenderIterations;
        private bool stopRequested;
        private bool isRunning;
        private DateTime last = DateTime.MinValue;
        private TimeSpan tickRate;
        private Queue<Interaction> interactionQueue;
        private ObservableObject observable = new ObservableObject();
        private FrameRateMeter frameRateMeter;
        // Events
        public Event<Exception> ExceptionOccurred { get; private set; } = new Event<Exception>();
        public Event<Thing> ThingRemoved { get; private set; } = new Event<Thing>();

  
        public Event<Thing> ThingAdded { get; private set; } = new Event<Thing>();
        public Event<Thing> ThingUpdated { get; private set; } = new Event<Thing>();
        public Event<Interaction> InteractionAdded { get; private set; } = new Event<Interaction>();
        public Event<Interaction> InteractionRemoved { get; private set; } = new Event<Interaction>();

        // Physical state of the scene
        public Rectangle Bounds { get; set; }
        public TimeSpan ElapsedTime { get; private set; }
        public IEnumerable<Interaction> Interactions
        {
            get
            {
                return _interactions;
            }
        }
        public IEnumerable<Thing> Things
        {
            get
            {
                return _things;
            }
        }
        public int FPS
        {
            get
            {
                return frameRateMeter != null ? frameRateMeter.CurrentFPS : 0;
            }
        }

        // Options
        public float SpeedFactor { get { return observable.Get<float>(); } set { observable.Set(value); } }
        public Action RenderImpl { get; set; }
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

        // Used to facilitate rendering
        public List<Thing> AddedSinceLastRender { get; private set; }
        public List<Thing> RemovedSinceLastRender { get; private set; }
        public List<Thing> UpdatedSinceLastRender { get; private set; }

        private Scene()
        {
            interactionQueue = new Queue<Interaction>();
            _things = new List<Thing>();
            AddedSinceLastRender = new List<Thing>();
            RemovedSinceLastRender = new List<Thing>();
            UpdatedSinceLastRender = new List<Thing>();
            _interactions = new List<Interaction>();
            ElapsedTime = TimeSpan.Zero;
            tickRate = TimeSpan.FromSeconds(.1);
            SpeedFactor = 1;
        }

        public Scene(float x, float y, float w, float h) : this()
        {
            Bounds = new Rectangle(x, y, w, h);
        }

        public void Clear()
        {
            while(_things.Count > 0)
            {
                Remove(_things[0]);
            }

            while (_interactions.Count > 0)
            {
                Remove(_interactions[0]);
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

        public void QueueAction(Action a)
        {
            lock(interactionQueue)
            {
                interactionQueue.Enqueue(new OneTimeInteraction(a));
            }
        }

        public void TogglePause()
        {
            if(isRunning)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }


        public Task Start()
        {
            if (RenderImpl == null) throw new InvalidOperationException($"You need to set the {nameof(RenderImpl)} property");

            stopRequested = false;
            last = DateTime.MinValue;

            return Task.Factory.StartNew(() =>
            {
                isRunning = true;
                stopRequested = false;
                Current = this;
                frameRateMeter = new FrameRateMeter();
                try
                {
                    while (!stopRequested)
                    {
                        lock (interactionQueue)
                        {
                            while (interactionQueue.Count > 0)
                            {
                                Add(interactionQueue.Dequeue());
                            }
                        }

                        while (DateTime.Now - last < _minTimeBetweenRenderIterations)
                        {
                            Thread.Sleep(0);
                        }
                        Tick();
                        RenderImpl();
                        AddedSinceLastRender.Clear();
                        RemovedSinceLastRender.Clear();
                        UpdatedSinceLastRender.Clear();
                        frameRateMeter.Increment();
                    }
                }
                catch (Exception ex)
                {
                    if (ExceptionOccurred.HasSubscriptions)
                    {
                        ExceptionOccurred.Fire(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    isRunning = false;
                    Current = null;
                }
            });
        }

        public void Stop()
        {
            QueueAction(() => { stopRequested = true; });
        }

        private void Tick()
        {
            DateTime now = DateTime.Now;
            if (last != DateTime.MinValue)
            {
                TimeSpan delta = now - last;
                delta = TimeSpan.FromSeconds(delta.TotalSeconds * SpeedFactor);
                tickRate = delta;
            }
            else
            {
                tickRate = TimeSpan.Zero;
            }

            last = now;
            MoveScene();
        }

        

        internal void MoveScene()
        {
            ElapsedTime += tickRate;

            for (int i = 0; i < _things.Count; i++)
            {
                var thing = _things[i];
                if (thing.Governor.ShouldFire(ElapsedTime))
                {
                    thing.Behave(this);
                    thing.LastBehavior = ElapsedTime;
                }
            }
            for (int i = 0; i < _interactions.Count; i++)
            {
                var interaction = _interactions[i];
                if (interaction.Governor.ShouldFire(ElapsedTime))
                {
                    interaction.Behave(this);
                    interaction.LastBehavior = ElapsedTime;
                }
            }
        }

        public void Add(params Interaction[] ints)
        {
            for (int i = 0; i < ints.Length; i++) Add(ints[i]);
        }

        public void Add(params Thing[] things)
        {
            for (int i = 0; i < things.Length; i++) Add(things[i]);
        }

        public void Add(Interaction i)
        {
            i.Id = NextId++;
            i.Scene = this;
            _interactions.Add(i);
            i.Initialize(this);
            i.LastBehavior = ElapsedTime;
            InteractionAdded.Fire(i);
            i.Added.Fire();
        }

        public void Remove(Interaction i)
        {
            if (_interactions.Remove(i))
            {
                InteractionRemoved.Fire(i);
                i.Removed.Fire();
                i.Scene = null;
                i.Dispose();
            }
        }

        public void Add(Thing t)
        {
            t.Scene = this;
            t.Id = NextId++;
            _things.Add(t);
            if (AddedSinceLastRender.Contains(t) == false) AddedSinceLastRender.Add(t);
            t.InitializeThing(this);
            ThingAdded.Fire(t);
            t.Added.Fire();
            t.LastBehavior = ElapsedTime;
        }

        public void Remove(Thing t)
        {
            if (_things.Remove(t))
            {
                RemovedSinceLastRender.Add(t);
                ThingRemoved.Fire(t);
                t.Removed.Fire();
                t.Scene = null;
                t.Dispose();
            }
        }

        public void Update(Thing t)
        {
            if (UpdatedSinceLastRender.Contains(t) == false) UpdatedSinceLastRender.Add(t);
            ThingUpdated.Fire(t);
            t.Updated.Fire();
        }
    }
}
