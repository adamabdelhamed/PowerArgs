using System.Diagnostics;
namespace PowerArgs.Cli.Physics;

public class Velocity2
{
    internal bool haveMovedSinceLastHitDetection = true;
    internal Angle angle;
    internal float speed;
    internal float lastEvalTime;

    public ColliderGroup Group { get; private set; }


    internal Event _onAngleChanged, _onSpeedChanged, _beforeMove, _onVelocityEnforced;
    internal Event<Impact> _impactOccurred;
    public Event OnAngleChanged { get => _onAngleChanged ?? (_onAngleChanged = new Event()); }
    public Event OnSpeedChanged { get => _onSpeedChanged ?? (_onSpeedChanged = new Event()); }
    public Event BeforeMove { get => _beforeMove ?? (_beforeMove = new Event()); }
    public Event OnVelocityEnforced { get => _onVelocityEnforced ?? (_onVelocityEnforced = new Event()); }
    public Event<Impact> ImpactOccurred { get => _impactOccurred ?? (_impactOccurred = new Event<Impact>()); }

    public Impact LastImpact { get; internal set; }
    public bool Bounce { get; set; }
    public HitPrediction NextCollision { get; internal set; }

    public Func<RectF> BoundsTransform { get; set; }
    public ICollider Collider { get; private set; }

    public float SpeedRatio { get; set; } = 1;

    public Angle Angle
    {
        get
        {
            return angle;
        }
        set
        {
            if (value == angle) return;
            angle = value;
            _onAngleChanged?.Fire();
        }
    }

    public float Speed
    {
        get
        {
            return speed;
        }
        set
        {
            if (value == speed) return;
            lastEvalTime = (float)Group.Now.TotalSeconds;
            speed = value;
            _onSpeedChanged?.Fire();
        }
    }

    public TimeSpan NextCollisionETA
    {
        get
        {
            if (NextCollision == null || Speed == 0 || NextCollision.Type == HitType.None) return TimeSpan.MaxValue;
            var d = NextCollision.LKGD;
            var seconds = d / speed;
            return TimeSpan.FromSeconds(seconds);
        }
    }

    public Velocity2(ICollider collider, ColliderGroup group)
    {
        this.Group = group;
        this.Collider = collider;
        group.Add(collider, this);
    }

    public Velocity2(ConsoleControl collider, ColliderGroup group) 
    {
        this.Group = group;
        this.Collider = collider;
        group.Add(collider, this);
        collider.OnDisposed(()=>
        {
           /*
            var lookup = this.Group.EnumerateCollidersSlow2(null).Where(c => c.c == collider).FirstOrDefault();
            if (lookup.c != Collider)
            {
                throw new NotSupportedException("OOPS, not there");
            }
           */
            if (this.Group.Remove(Collider) == false)
            {
                throw new InvalidOperationException($"Failed to remove myself from group after dispose: {collider.GetType().Name}-{collider.ColliderHashCode}");
            }
        });
    }

    public ILifetimeManager CreateVelocityChangedLifetime() => 
        Lifetime.EarliestOf(OnSpeedChanged.CreateNextFireLifetime(), OnAngleChanged.CreateNextFireLifetime()).Manager;
    

    public IEnumerable<ICollider> GetObstaclesSlow(List<ICollider> buffer = null) => Group.GetObstaclesSlow(Collider, buffer);
    public void Stop() => Speed = 0;
}

public class ColliderGroup
{
    private int NextHashCode = 0;
    public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();
    public int Count { get; private set; }
    private VelocityHashTable velocities;
    public float LatestDT { get; private set; }

    // these properties model a linear progression that determines the appropriate min
    // evaluation time period for an object given it's current speed
    private const float LeastFrequentEval = .1f; // y1
    private const float LowestSpeedForEvalCalc = 0; // x1
    private const float MostFrequentEval = .025f; // y2
    private const float HighestSpeedForEvalCalc = 60; // x2
    private const float EvalFrequencySlope = (MostFrequentEval - LeastFrequentEval) / (HighestSpeedForEvalCalc - LowestSpeedForEvalCalc);

    private int colliderBufferLength;
    private ICollider[] colliderBuffer;
    private RectF[] obstacleBuffer;
    private HitPrediction hitPrediction;
    private ILifetimeManager lt;
    private TimeSpan lastExecuteTime;


    private Event<(Velocity2 Velocity, ICollider Collider)> _added;
    public Event<(Velocity2 Velocity, ICollider Collider)> Added { get => _added ?? (_added = new Event<(Velocity2 Velocity, ICollider Collider)>()); }

    private Event<(Velocity2 Velocity, ICollider Collider)> _removed;
    public Event<(Velocity2 Velocity, ICollider Collider)> Removed { get => _removed ?? (_removed = new Event<(Velocity2 Velocity, ICollider Collider)>()); }

    public float SpeedRatio { get; set; } = 1;

    public PauseManager PauseManager { get; set; }

    public ColliderGroup(ILifetimeManager lt)
    {
        this.lt = lt;
        hitPrediction = new HitPrediction();
        ConsoleApp.Current.Invoke(ExecuteAsync);
    }

    public bool TryLookupVelocity(ICollider c, out Velocity2 v) => velocities.TryGetValue(c, out v);

    internal (int RowIndex, int ColIndex) Add(ICollider c, Velocity2 v)
    {
        if(c.ColliderHashCode >= 0)
        {
            throw new System.Exception("Already has a hashcode");
        }
        c.ColliderHashCode = NextHashCode++;
        if (Count == colliderBuffer.Length)
        {
            var tmp = colliderBuffer;
            colliderBuffer = new ICollider[tmp.Length * 2];
            Array.Copy(tmp, colliderBuffer, tmp.Length);

            var tmp2 = obstacleBuffer;
            obstacleBuffer = new RectF[tmp.Length * 2];
            Array.Copy(tmp2, obstacleBuffer, tmp2.Length);
        }
        v.lastEvalTime = (float)lastExecuteTime.TotalSeconds;
        var ret = velocities.Add(c, v);
        Count++;
        _added?.Fire((v, c));
        return ret;
    }

    public bool Remove(ICollider c)
    {
        if(velocities.Remove(c, out Velocity2 v))
        {
            _removed?.Fire((v, c));
            Count--;
            return true;
        }
        return false;
    }

    public TimeSpan Now => stopwatch.Elapsed;
    private Stopwatch stopwatch;
    private async Task ExecuteAsync()
    {
        velocities = new VelocityHashTable();
        colliderBufferLength = 0;
        colliderBuffer = new ICollider[100];
        obstacleBuffer = new RectF[100];
        stopwatch = Stopwatch.StartNew();
        lastExecuteTime = TimeSpan.Zero;

        while (lt.IsExpired == false)
        {
            await Task.Yield();

            if (PauseManager?.State == PauseManager.PauseState.Paused)
            {
                stopwatch.Stop();
                while (PauseManager.State == PauseManager.PauseState.Paused)
                {
                    await Task.Yield();
                }
                stopwatch.Start();
            }

            var now = stopwatch.Elapsed;
            LatestDT = (float)(now - lastExecuteTime).TotalMilliseconds;
            lastExecuteTime = now;
            Tick((float)now.TotalSeconds);
        }
    }

    private void Tick(float now)
    {
        CalcObstacles();
        var vSpan = velocities.Table.AsSpan();
        for (var i = 0; i < vSpan.Length; i++)
        {
            var entry = vSpan[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                // item is null if our sparse hashtable is empty in this spot
                if (item == null) continue;

                // no need to evaluate this velocity if it's not moving
                var velocity = item.Velocity;
                if (velocity.Speed <= 0) continue;

                // Tick can happen very frequently, but velocities that are moving slowly don't
                // need to be evaluated as frequently. These next few lines will use a linear model to determine
                // the appropriate time to wait between evaluations, based on the object's speed
                var evalFrequency = SpeedRatio * velocity.SpeedRatio * (velocity.Speed > HighestSpeedForEvalCalc ? .025f : EvalFrequencySlope * velocity.speed + LeastFrequentEval);
                var minEvalTime = velocity.lastEvalTime + evalFrequency;
                if (now < minEvalTime) continue;
                var dt = ((float)now - velocity.lastEvalTime) * SpeedRatio * velocity.SpeedRatio;
                velocity.lastEvalTime = now;

                // before moving the object, see if the movement would impact another object
                float d = velocity.Speed * dt;
                var bounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : item.Collider.Bounds;
                hitPrediction.Clear();
                HitDetection.PredictHitFast(velocity.Collider, bounds, obstacleBuffer, velocity.Angle, colliderBuffer, 1.5f * d, CastingMode.Precise, colliderBufferLength, hitPrediction);
                velocity.NextCollision = hitPrediction;
                velocity._beforeMove?.Fire();

                if (hitPrediction.Type != HitType.None && hitPrediction.LKGD <= d)
                {
                    var obstacleHit = hitPrediction.ColliderHit;
                    var dx = velocity.BoundsTransform != null ? bounds.Left - item.Collider.Left() : 0;
                    var dy = velocity.BoundsTransform != null ? bounds.Top - item.Collider.Top() : 0;

                    var proposedBounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : item.Collider.Bounds;
                    var distanceToObstacleHit = proposedBounds.CalculateDistanceTo(obstacleHit.Bounds);
                   
                        proposedBounds = proposedBounds.OffsetByAngleAndDistance(velocity.Angle, distanceToObstacleHit - .5f, false);
                        item.Collider.Bounds = new RectF(proposedBounds.Left - dx, proposedBounds.Top - dy, item.Collider.Width(), item.Collider.Height());
                        velocity.haveMovedSinceLastHitDetection = true;
                    
                    var angle = bounds.CalculateAngleTo(obstacleHit.Bounds);

                    if (velocity.haveMovedSinceLastHitDetection)
                    {
                        velocity.LastImpact = new Impact()
                        {
                            Angle = angle,
                            MovingObject = item.Collider,
                            ColliderHit = obstacleHit,
                            HitType = hitPrediction.Type,
                            Prediction = hitPrediction,
                        };

                        if (velocities.TryGetValue(obstacleHit, out Velocity2 vOther))
                        {
                            if(vOther.Bounce)
                            {
                                var topOrBottomEdgeWasHit = hitPrediction.Edge == obstacleHit.Bounds.TopEdge || hitPrediction.Edge == obstacleHit.Bounds.BottomEdge;
                                vOther.Angle = topOrBottomEdgeWasHit ? Angle.Right.Add(-vOther.Angle.Value) : Angle.Left.Add(-vOther.Angle.Value);
                            }

                            vOther._impactOccurred?.Fire(new Impact()
                            {
                                Angle = angle.Opposite(),
                                MovingObject = obstacleHit,
                                ColliderHit = item.Collider,
                                HitType = hitPrediction.Type,
                            });
                        }

                        velocity._impactOccurred?.Fire(velocity.LastImpact);
                        ImpactOccurred.Fire(velocity.LastImpact);
                        velocity.haveMovedSinceLastHitDetection = false;
                    }

                    if (velocity.Bounce)
                    {
                        var topOrBottomEdgeWasHit = hitPrediction.Edge == obstacleHit.Bounds.TopEdge || hitPrediction.Edge == obstacleHit.Bounds.BottomEdge;
                        velocity.Angle = topOrBottomEdgeWasHit ? Angle.Right.Add(-velocity.Angle.Value) : Angle.Left.Add(-velocity.Angle.Value);
                    }
                    else
                    {
                        velocity.Stop();
                    }
                }
                else
                {
                    var newLocation = item.Collider.Bounds.OffsetByAngleAndDistance(velocity.Angle, d);
                    item.Collider.Bounds = newLocation;
                    velocity.haveMovedSinceLastHitDetection = true;
                }

                velocity._onVelocityEnforced?.Fire();
            }
        }
    }

    private void CalcObstacles()
    {
        colliderBufferLength = 0;
        var colliderBufferSpan = colliderBuffer.AsSpan();
        var obstacleBufferSpan = obstacleBuffer.AsSpan();
        var span = velocities.Table.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                if (item == null) continue;

                colliderBufferSpan[colliderBufferLength] = item.Collider;
                obstacleBufferSpan[colliderBufferLength] = item.Collider.Bounds;
                colliderBufferLength++;
            }
        }
    }

    public IEnumerable<ICollider> EnumerateCollidersSlow(List<ICollider> list = null)
    {
        list = list ?? new List<ICollider>(Count);
        colliderBufferLength = 0;
        var span = velocities.Table.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                if (item == null) continue;
                list.Add(item.Collider);
            }
        }
        return list;
    }

    public IEnumerable<ICollider> EnumerateCollidersSlow(List<ICollider> list = null, ICollider except = null)
    {
        list = list ?? new List<ICollider>(Count);
        colliderBufferLength = 0;
        var span = velocities.Table.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                if (item == null || item.Collider == except) continue;
                list.Add(item.Collider);
            }
        }
        return list;
    }

    public IEnumerable<ICollider> GetObstaclesSlow(ICollider owner, List<ICollider> list = null)
    {
        list = list ?? new List<ICollider>(Count);
        colliderBufferLength = 0;
        var span = velocities.Table.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                if (item == null || item.Collider == owner || owner.CanCollideWith(item.Collider) == false) continue;
                list.Add(item.Collider);
            }
        }
        return list;
    }

    private class VelocityHashTable
    {
        public class Item
        {
            public readonly ICollider Collider;
            public readonly Velocity2 Velocity;

            public Item(ICollider c, Velocity2 v)
            {
                Collider = c;
                Velocity = v;
            }
        }

        public Item[][] Table;

        public VelocityHashTable()
        {
            Table = new Item[300][];
            var span = Table.AsSpan();
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = new Item[4];
            }
        }

        public void ValidateEntries()
        {
            for(var i = 0; i < Table.Length; i++)
            {
                for(var j = 0; j < Table[i].Length; j++)
                {
                    var entry = Table[i][j];
                    if (entry == null) continue;
                    var correctIndex = entry.Collider.ColliderHashCode % Table.Length;
                    if(correctIndex != i)
                    {
                        throw new System.Exception($"Item in the wrong place: Expected: {correctIndex}, Actual: {i}");
                    }
                }
            }
        }

        internal (int RowIndex, int ColIndex) Add(ICollider c, Velocity2 v)
        {
            var i = c.ColliderHashCode % Table.Length;
            var myArray = Table[i].AsSpan();
            for (var j = 0; j < myArray.Length; j++)
            {
                if (myArray[j] == null)
                {
                    myArray[j] = new Item(c, v);
                    return (i,j);
                }
            }
            var biggerArray = new Item[myArray.Length * 2];
            Array.Copy(Table[i], biggerArray, myArray.Length);
            biggerArray[myArray.Length] = new Item(c, v);
            Table[i] = biggerArray;
            return (i, myArray.Length);
        }

        public bool Remove(ICollider c, out Velocity2 v)
        {
            var i = c.ColliderHashCode % Table.Length;
            var myArray = Table[i].AsSpan();
            for (var j = 0; j < myArray.Length; j++)
            {
                if (ReferenceEquals(c, myArray[j]?.Collider))
                {
                    v = myArray[j].Velocity;
                    myArray[j] = null;
                    for (var k = j; k < myArray.Length - 1; k++)
                    {
                        myArray[k] = myArray[k + 1];
                        myArray[k+1] = null;
                        if (myArray[k] == null) break;
                    }
                    return true;
                }
            }
            v = null;
            return false;
        }

        public bool TryGetValue(ICollider c, out Velocity2 v)
        {
            var i = c.ColliderHashCode % Table.Length;
            var myArray = Table[i].AsSpan();
            for (var j = 0; j < myArray.Length; j++)
            {
                var item = myArray[j];
                if (item == null)
                {
                    v = null;
                    return false;
                }

                if (ReferenceEquals(c, item.Collider))
                {
                    v = item.Velocity;
                    return true;
                }
            }
            v = null;
            return false;
        }
    }
}