using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public enum ProximityMineState
    {
        NoNearbyThreats,
        ThreatApproaching,
        ThreatNearby
    }

    public class ProximityMineWatcher : AsyncTimeFunction
    {
        public ProximityMineWatcher()
        {
            Start();
        }

        protected override async Task ExecuteAsync()
        {
            while(Lifetime.IsExpired == false)
            {
                var mines = SpaceTime.CurrentSpaceTime.Elements.WhereAs<ProximityMine>().ToArray();
                if (mines.Length == 0)
                {
                    Lifetime.Dispose();
                    break;
                }

                foreach(var mineGroup in mines.GroupBy(m => m.TargetTag + "/"+m.ZIndex))
                {
                    var proto = mineGroup.First();
                    var tag = proto.TargetTag;
                    var z = proto.ZIndex;
                    var targets = SpaceTime.CurrentSpaceTime.Elements
                        .Where(e => e.ZIndex == z && e.Tags.Contains(tag)).ToArray();

                    foreach(var mine in mineGroup)
                    {
                        if (mine.Lifetime.IsExpired) continue;

                        var closest = targets.OrderBy(t => Geometry.CalculateNormalizedDistanceTo(mine, t)).FirstOrDefault();

                        if (closest == null)
                        {
                            mine.State = ProximityMineState.NoNearbyThreats;
                            continue;
                        }

                        var d = Geometry.CalculateNormalizedDistanceTo(closest, mine);

                        if (d < mine.Range * .9f)
                        {
                            mine.Explode();
                        }
                        else if (d < mine.Range * 3f)
                        {
                            mine.State = ProximityMineState.ThreatNearby;
                        }
                        else if (d < mine.Range * 6f)
                        {
                            mine.State = ProximityMineState.ThreatApproaching;
                        }
                        else
                        {
                            mine.State = ProximityMineState.NoNearbyThreats;
                        }
                        await Time.CurrentTime.YieldAsync();
                    }

                    await Time.CurrentTime.DelayFuzzyAsync(333);
                }
            }
        }
    }

    public class ProximityMine : Explosive
    {
        public string TargetTag { get; set; }
        public ProximityMineState State { get; set; } = ProximityMineState.NoNearbyThreats;


       
        public ProximityMine(Weapon w) : base(w)
        {
            this.Governor.Rate = TimeSpan.FromSeconds(-1);

            if(Time.CurrentTime.Functions.WhereAs<ProximityMineWatcher>().None())
            {
                Time.CurrentTime.Add(new ProximityMineWatcher());
            }
        }
    }

    [SpacialElementBinding(typeof(ProximityMine))]
    public class ProximityMineRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            var state = (Element as ProximityMine).State;

            if(state == ProximityMineState.NoNearbyThreats)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.DarkGray), 0, 0, Width, Height);
            }
            else if(state == ProximityMineState.ThreatApproaching)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.Black, ConsoleColor.DarkYellow), 0, 0, Width, Height);
            }
            else if(state == ProximityMineState.ThreatNearby)
            {
                context.FillRect(new ConsoleCharacter('#', ConsoleColor.Black, ConsoleColor.Red), 0, 0, Width, Height);
            }
        }
    }
}
