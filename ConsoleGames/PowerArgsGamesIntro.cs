using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleGames
{
    public class PowerArgsGamesIntro : SpacetimePanel
    {
        private Deferred d;
        private Level level;
        private SceneFactory factory;
        private Character character;

        public PowerArgsGamesIntro() : base(52, 7)
        {
            Background = ConsoleColor.Black;
            level = LevelEditor.LoadBySimpleName("PowerArgsIntro");
            factory = new SceneFactory(new List<ItemReviver>() { new LetterReviver() });
            AddedToVisualTree.SubscribeOnce(() =>
            {
                this.CenterBoth();

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, () =>
                {
                    SpaceTime.Stop();
                    if (d.IsFulfilled == false)
                    {
                        d.Resolve();
                    }

                    if(this.IsExpired == false)
                    {
                        (this.Parent as ConsolePanel).Controls.Remove(this);
                    }
                }, this);

            });

        }
        public Promise Play()
        {
            d = Deferred.Create();
            SpaceTime.QueueAction(PlaySceneInternal);
            SpaceTime.Start();
            return d.Promise;
        }

        private void PlaySceneInternal()
        {
            // reveal the PowerArgs logo
            factory.InitializeScene(level).ForEach(e => SpaceTime.Add(e));
            // create the character
            character = new MainCharacter();
            // he starts a few pixels from the right edge
            character.MoveTo(Width - 7, 0);
            // he moves to the right
            character.Speed.SpeedX = 5;
            // he drops a timed mine and turns around when he gets near the right edge
            ListenForCharacterNearRightEdge();

            SpaceTime.Add(character);

            ListenForEndOfIntro();
        }

        private void ListenForCharacterNearRightEdge()
        {
            ITimeFunction watcher = null;
            watcher = TimeFunction.Create(() =>
            {
                if (character.Left > Width - 2)
                {
                    // turn the character around so he now moves to the left
                    character.Speed.SpeedX = -7;

                    // drop a timed mine
                    var dropper = new TimedMineDropper() { Delay = TimeSpan.FromSeconds(4.5), AmmoAmount = 1, Holder = character };
                    dropper.Exploded.SubscribeOnce(() => Sound.Play("PowerArgsIntro"));
                    dropper.FireInternal();

                    // eventually he will hit the left wall, remove him when that happens
                    character.Speed.ImpactOccurred.SubscribeForLifetime((i) =>  character.Lifetime.Dispose(), character.Lifetime);

                    // this watcher has done its job, stop watching the secne 
                    watcher.Lifetime.Dispose();
                }
            });
            SpaceTime.Add(watcher);
        }

        private void ListenForEndOfIntro()
        {
            SpaceTime.Add(TimeFunction.Create(() =>
            {
                var remainingCount = SpaceTime.Elements.Where(e => e is FlammableLetter || e is Fire).Count();
                if (remainingCount == 0)
                {
                    SpaceTime.Elements.ForEach(e => e.Lifetime.Dispose());
                    SpaceTime.Stop();

                    if (d.IsFulfilled == false)
                    {
                        d.Resolve();
                    }

                    if (this.IsExpired == false)
                    {
                        this.Dispose();
                    }
                }
            }));
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
                hydratedElement = new FlammableLetter() { Symbol = new ConsoleCharacter(item.Symbol, ConsoleColor.White, item.BG) };
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
            this.HealthPoints = SpaceTime.CurrentSpaceTime.Random.Next(40, 60);
        }

        public override void Evaluate()
        {
            Fire.BurnIfTouchingSomethingHot(this, TimeSpan.FromSeconds(.1), this.Symbol.Value);
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
