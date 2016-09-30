using System;
using System.Collections.Generic;

namespace PowerArgs.Cli.Physics
{
    public class Realm
    { 
        public Event<Thing> ThingRemoved { get; private set; } = new Event<Thing>();
        public Event<Thing> ThingAdded { get; private set; } = new Event<Thing>();
        public Event<Thing> ThingUpdated { get; private set; } = new Event<Thing>();
        public Event<Interaction> InteractionAdded { get; private set; } = new Event<Interaction>();
        public Event<Interaction> InteractionRemoved { get; private set; } = new Event<Interaction>();


        private List<Thing> _things;
        private List<Interaction> _interactions;
        private long NextId;

        public Rectangle Bounds { get; set; }
        public TimeSpan ElapsedTime { get; private set; }
        public TimeSpan TickRate { get; internal set; }

        public List<Thing> Added { get; private set; }
        public List<Thing> Removed { get; private set; }
        public List<Thing> Updated { get; private set; }

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

        public bool HasChanged
        {
            get
            {
                return Added.Count > 0 || Removed.Count > 0 || Updated.Count > 0;
            }
        }

        private Realm()
        {
            _things = new List<Thing>();
            Added = new List<Thing>();
            Removed = new List<Thing>();
            Updated = new List<Thing>();
            _interactions = new List<Interaction>();
            ElapsedTime = TimeSpan.Zero;
            TickRate = TimeSpan.FromSeconds(.1);
        }

        public Realm(float x, float y, float w, float h) : this()
        {
            Bounds = new Rectangle(x, y, w, h);
        }

        internal void Tick()
        {
            ElapsedTime += TickRate;

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
            i.Realm = this;
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
                i.Realm = null;
                i.Dispose();
            }
        }

        public void Add(Thing t)
        {
            t.Realm = this;
            t.Id = NextId++;
            _things.Add(t);
            if (Added.Contains(t) == false) Added.Add(t);
            t.InitializeThing(this);
            ThingAdded.Fire(t);
            t.Added.Fire();
            t.LastBehavior = ElapsedTime;
        }

        public void Remove(Thing t)
        {
            if (_things.Remove(t))
            {
                Removed.Add(t);
                ThingRemoved.Fire(t);
                t.Removed.Fire();
                t.Realm = null;
                t.Dispose();
            }
        }

        public void Update(Thing t)
        {
            if (Updated.Contains(t) == false) Updated.Add(t);
            ThingUpdated.Fire(t);
            t.Updated.Fire();
        }
    }
}
