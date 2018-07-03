using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using PowerArgs.Cli;

namespace ConsoleGames
{
    public class Net : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal()
        {
            var angle = Holder.Target != null ? Holder.CalculateAngleTo(Holder.Target) : Holder.Speed.Angle;

            // todo - make it so each weapon does not need to be main character aim aware
            if(Holder is MainCharacter && (Holder as MainCharacter).AimMode == AimMode.Manual)
            {
                angle =  Holder.CalculateAngleTo((Holder as MainCharacter).FreeAimCursor);
            }

            var matterList = new List<NetMatter>();
            StructuralIntegrity<NetMatter> matterIntegrity = null;
            for (var y = 0; y < 3; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    var matter = new NetMatter();

                    var force = new Force(matter.Speed, 45, angle);
                    var matterX = this.Holder.Left + 1 + x;
                    var matterY = this.Holder.Top - 1 + y;
                    matter.MoveTo(matterX, matterY, 1);
                    matterList.Add(matter);
 
                    matter.Speed.HitDetectionTypes.Add(typeof(Character));
                    matter.Speed.HitDetectionTypes.Add(typeof(Wall));

                    // wraps the character in the net if it hits them
                    matter.Speed.ImpactOccurred.SubscribeForLifetime((i) =>
                    {
                        if (i.ElementHit != Holder && i.ElementHit is Character)
                        {
                            matterList.ForEach(m => m.Lifetime.Dispose());
                            matterList.Clear();
                            matterIntegrity.Lifetime.Dispose();
                            for(var newX = i.ElementHit.Left-2; newX <= i.ElementHit.Left+2; newX++ )
                            {
                                for (var newY = i.ElementHit.Top - 1; newY <= i.ElementHit.Top+ 1; newY++)
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

        public class NetMatter : SpacialElement, IDestructible
        {
            internal List<NetMatter> Composite { get; set; }

            public Event Damaged { get; private set; } = new Event();

            public Event Destroyed { get; private set; } = new Event();

            public float HealthPoints { get; set; } = 5;

            public SpeedTracker Speed { get; private set; } 

            private IRectangular initialBonds;
            private TimeSpan initialTime;

            public NetMatter()
            {
                Speed = new SpeedTracker(this);
            }

            public override void Initialize()
            {
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
                            matter.Speed.SpeedX = 0;
                            matter.Speed.SpeedY = 0;
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
