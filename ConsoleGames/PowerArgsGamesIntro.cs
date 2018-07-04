using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using PowerArgs;
using System.Linq;

namespace ConsoleGames
{
    public class PowerArgsGamesIntro : SpacetimePanel
    {
        public PowerArgsGamesIntro() : base(52, 7)
        {
            Background = ConsoleColor.Black;
        }
        public Promise Play()
        {
            Deferred d = Deferred.Create();

            SpaceTime.QueueAction(() =>
            {
                var level = LevelEditor.LoadBySimpleName("PowerArgsIntro");
                SceneFactory factory = new SceneFactory(new List<ItemReviver>()
                {
                    new LetterReviver()
                });

                var dummyCharacter = new Character();
                dummyCharacter.MoveTo(Width-1, 0);
                var dropper = new TimedMineDropper() { Delay = TimeSpan.FromSeconds(4.5), AmmoAmount = 1, Holder = dummyCharacter };
                dropper.Exploded.SubscribeOnce(()=>Sound.Play("PowerArgsIntro"));

                dropper.FireInternal();
                factory.InitializeScene(level).ForEach(e => SpaceTime.Add(e));

                var playedFinalBurn = false;
                SpaceTime.Add(TimeFunction.Create(() =>
                {
                    var remainingCount = SpaceTime.Elements.Where(e => e is FlammableLetter || e is Fire).Count();
                    if (remainingCount == 0)
                    {
                        SpaceTime.Elements.ForEach(e => e.Lifetime.Dispose());
                        SpaceTime.Stop();
                        d.Resolve();
                    }
                    else if(remainingCount < 50 && playedFinalBurn == false)
                    {
                        Sound.Play("burn");
                        playedFinalBurn = true;
                    }
                }));
            });
            

            SpaceTime.Start();
            return d.Promise;
        }
    }

    public class LetterReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            if(item.FG != ConsoleColor.Red)
            {
                hydratedElement = null;
                return false;
            }
            else
            {
                hydratedElement = new FlammableLetter() { Symbol = new ConsoleCharacter(item.Symbol, item.FG, item.BG) };
                return true;
            }
        }
    }

    public class FlammableLetter : SpacialElement, IDestructible
    {
        public ConsoleCharacter Symbol { get; set; }

        public Event Damaged { get; private set; } = new Event();

        public Event Destroyed { get; private set; } = new Event();

        public float HealthPoints { get; set; }

        public override void Initialize()
        {
            this.HealthPoints = SpaceTime.CurrentSpaceTime.Random.Next(30, 100);
        }

        public override void Evaluate()
        {
            Fire.BurnIfTouchingSomethingHot(this, TimeSpan.FromSeconds(.1));
        }
    }

    [SpacialElementBinding(typeof(FlammableLetter))]
    public class FlammableLetterRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = (Element as FlammableLetter).Symbol;
            context.FillRect(0, 0, Width, Height);
        }
    }
}
