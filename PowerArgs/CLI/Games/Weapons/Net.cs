using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class NetWrappedEventArgs
    {
        public Net Net { get; set; }
        public Character Wrapped { get; set; }
    }

    public class Net : Weapon
    {
        public static Event<NetWrappedEventArgs> OnWrappedCharacter { get; private set; } = new Event<NetWrappedEventArgs>();
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal(bool alt)
        {
            var matterList = new List<NetMatter>();
            StructuralIntegrity<NetMatter> matterIntegrity = null;
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    var matter = new NetMatter();

                    var force = new Force(matter.Speed, 45f.NormalizeQuantity(CalculateAngleToTarget()), CalculateAngleToTarget());
                    var matterX = this.Holder.Left + 1 + x;
                    var matterY = this.Holder.Top - 1 + y;
                    matter.MoveTo(matterX, matterY, 1);
                    matterList.Add(matter);
 
                    // wraps the character in the net if it hits them
                    matter.Speed.ImpactOccurred.SubscribeForLifetime((i) =>
                    {
                        if (i.ObstacleHit != Holder && i.ObstacleHit is Character)
                        {
                            OnWrappedCharacter.Fire(new NetWrappedEventArgs() { Net = this, Wrapped = i.ObstacleHit as Character });
                            matterList.ForEach(m => m.Lifetime.Dispose());
                            matterList.Clear();
                            matterIntegrity.Lifetime.Dispose();
                            for(var newX = i.ObstacleHit.Left-2; newX <= i.ObstacleHit.Left+2; newX++ )
                            {
                                for (var newY = i.ObstacleHit.Top - 1; newY <= i.ObstacleHit.Top+ 1; newY++)
                                {
                                    var newMatter = new NetMatter();
                                    newMatter.MoveTo(newX, newY);
                                    matterList.Add(newMatter);
                                }
                            }

                            matterList.ForEach(m =>
                            {
                                m.Composite = matterList;
                                SpaceTime.CurrentSpaceTime.Add(m);
                            });
                        }
                    }, matter.Lifetime);
                    
                }
            }

            matterList.ForEach(m =>
            {
                m.Composite = matterList;
                SpaceTime.CurrentSpaceTime.Add(m);
            });

            matterIntegrity = new StructuralIntegrity<NetMatter>(matterList);
            SpaceTime.CurrentSpaceTime.Add(matterIntegrity);
        }

        public class NetMatter : SpacialElement
        {
            internal List<NetMatter> Composite { get; set; }

            public Velocity Speed { get; private set; } 

            private IRectangularF initialBonds;
            private TimeSpan initialTime;

            public NetMatter()
            {
                Speed = new Velocity(this);
                this.initialBonds = this.CopyBounds();
                this.initialTime = Time.CurrentTime.Now;
            }

            public override void Evaluate()
            {

                if (Time.CurrentTime.Now - initialTime > TimeSpan.FromSeconds(5))
                {
                    foreach (var matter in Composite)
                    {
                        matter.Lifetime.Dispose();
                    }
                }
                else
                {
                    Fire.BurnIfTouchingSomethingHot(this);
                    var distance = this.CalculateDistanceTo(initialBonds);
                    if (distance > 10)
                    {
                        foreach (var matter in Composite)
                        {
                            matter.Speed.Stop();
                        }
                    }
                }
            }
        }

        [SpacialElementBinding(typeof(NetMatter))]
        public class NetMatterRenderer : SpacialElementRenderer
        {
            protected override void OnPaint(ConsoleBitmap context)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('#', ConsoleColor.DarkYellow, ConsoleColor.Black);
                context.DrawPoint(0, 0);
            }
        }
    }
}
