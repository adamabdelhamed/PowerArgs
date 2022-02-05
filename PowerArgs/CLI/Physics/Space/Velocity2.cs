using System.Diagnostics;
namespace PowerArgs.Cli.Physics;

public class Velocity2
{
    internal bool haveMovedSinceLastHitDetection = true;
    internal Angle angle;
    internal float speed;
    internal float lastEvalTime;

    public Event OnAngleChanged { get; private set; } = new Event();
    public Event OnSpeedChanged { get; private set; } = new Event();
    public Event BeforeMove { get; private set; } = new Event();
    public Event OnVelocityEnforced { get; private set; } = new Event();
    public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();

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
            OnAngleChanged.Fire();
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
            speed = value;
            OnSpeedChanged.Fire();
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
        this.Collider = collider;
        group.Add(collider, this);
    }

    public Velocity2(ConsoleControl collider, ColliderGroup group) : this((ICollider)collider, group)
    {
        collider.OnDisposed(() => group.Remove(collider));
    }

    public void Stop() => Speed = 0;
}

public class ColliderGroup
{
    public Event<Impact> ImpactOccurred { get; private set; } = new Event<Impact>();

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
    private int maxCapacity;
    private TimeSpan lastExecuteTime;

    public float SpeedRatio { get; set; } = 1;

    public PauseManager PauseManager { get; set; }

    public ColliderGroup(ILifetimeManager lt, int maxCapacity = 2500)
    {
        this.lt = lt;
        this.maxCapacity = maxCapacity;
        hitPrediction = new HitPrediction();
        ConsoleApp.Current.Invoke(ExecuteAsync);
    }

    internal void Add(ICollider c, Velocity2 v)
    {
        v.lastEvalTime = (float)lastExecuteTime.TotalSeconds;
        velocities.Add(c, v);
    }
    
    internal void Remove(ICollider c) => velocities.Remove(c);
    private async Task ExecuteAsync()
    {
        velocities = new VelocityHashTable();
        colliderBufferLength = 0;
        colliderBuffer = new ICollider[maxCapacity];
        obstacleBuffer = new RectF[maxCapacity];
        var stopwatch = Stopwatch.StartNew();
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
                CalcObstacles(item.Collider);
                var bounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : item.Collider.Bounds;
                hitPrediction.Clear();
                HitDetection.PredictHit(bounds, obstacleBuffer, velocity.Angle, colliderBuffer, 1.5f * d, bufferLen: colliderBufferLength, toReuse: hitPrediction);
                velocity.NextCollision = hitPrediction;
                velocity.BeforeMove.Fire();

                if (hitPrediction.Type != HitType.None && hitPrediction.LKGD <= d)
                {
                    var obstacleHit = hitPrediction.ColliderHit;
                    var dx = velocity.BoundsTransform != null ? bounds.Left - item.Collider.Left() : 0;
                    var dy = velocity.BoundsTransform != null ? bounds.Top - item.Collider.Top() : 0;

                    var proposedBounds = velocity.BoundsTransform != null ? velocity.BoundsTransform() : item.Collider.Bounds;
                    var distanceToObstacleHit = proposedBounds.CalculateDistanceTo(obstacleHit.Bounds);
                    if (distanceToObstacleHit > .5f)
                    {
                        proposedBounds = proposedBounds.OffsetByAngleAndDistance(velocity.Angle, distanceToObstacleHit - .5f, false);
                        item.Collider.Bounds = new RectF(proposedBounds.Left - dx, proposedBounds.Top - dy, item.Collider.Width(), item.Collider.Height());
                        velocity.haveMovedSinceLastHitDetection = true;
                    }
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
                                vOther.angle = topOrBottomEdgeWasHit ? Angle.Right.Add(-vOther.Angle.Value) : Angle.Left.Add(-vOther.Angle.Value);
                            }

                            vOther.ImpactOccurred.Fire(new Impact()
                            {
                                Angle = angle.Opposite(),
                                MovingObject = obstacleHit,
                                ColliderHit = item.Collider,
                                HitType = hitPrediction.Type,
                            });
                        }

                        velocity.ImpactOccurred?.Fire(velocity.LastImpact);
                        ImpactOccurred.Fire(velocity.LastImpact);
                        velocity.haveMovedSinceLastHitDetection = false;
                    }

                    if (velocity.Bounce)
                    {
                        var topOrBottomEdgeWasHit = hitPrediction.Edge == obstacleHit.Bounds.TopEdge || hitPrediction.Edge == obstacleHit.Bounds.BottomEdge;
                        velocity.angle = topOrBottomEdgeWasHit ? Angle.Right.Add(-velocity.Angle.Value) : Angle.Left.Add(-velocity.Angle.Value);
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

                velocity.OnVelocityEnforced?.Fire();
            }
        }
    }

    private void CalcObstacles(ICollider collider)
    {
        colliderBufferLength = 0;
        var span = velocities.Table.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            var entry = span[i].AsSpan();
            for (var j = 0; j < entry.Length; j++)
            {
                var item = entry[j];
                if (item == null) continue;

                if (item.Collider != collider && collider.CanCollideWith(item.Collider))
                {
                    colliderBuffer[colliderBufferLength] = item.Collider;
                    obstacleBuffer[colliderBufferLength] = item.Collider.Bounds;
                    colliderBufferLength++;
                }
            }
        }
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

        public void Add(ICollider c, Velocity2 v)
        {
            var i = c.ColliderHashCode % Table.Length;
            var myArray = Table[i].AsSpan();
            for (var j = 0; j < myArray.Length; j++)
            {
                if (myArray[j] == null)
                {
                    myArray[j] = new Item(c, v);
                    return;
                }
            }

            var temp = myArray;
            var newArray = new Item[temp.Length * 2];
            Array.Copy(Table[i], newArray, temp.Length);
            newArray[temp.Length] = new Item(c, v);
            Table[i] = newArray;
        }

        public void Remove(ICollider c)
        {
            var i = c.ColliderHashCode % Table.Length;
            var myArray = Table[i].AsSpan();
            for (var j = 0; j < myArray.Length; j++)
            {
                if (ReferenceEquals(c, myArray[j]?.Collider))
                {
                    myArray[j] = null;
                    for (var k = j; k < myArray.Length - 1; k++)
                    {
                        myArray[k] = myArray[k + 1];
                        myArray[k+1] = null;
                        if (myArray[k] == null) break;
                    }
                    break;
                }
            }
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