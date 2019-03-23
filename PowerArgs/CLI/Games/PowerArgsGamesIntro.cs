using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Games
{
    public class PowerArgsGamesIntro : SpacetimePanel
    {
        private Deferred introDeferred;
        private static readonly Level level = new GeneratedLevels.PowerArgsGameIntroSeed();
        private SceneFactory factory;
        private Character character;

        public PowerArgsGamesIntro() : base(52, 7)
        {
            Background = ConsoleColor.Black;
            factory = new SceneFactory(new List<ItemReviver>() { new LetterReviver() });
            AddedToVisualTree.SubscribeOnce(() =>
            {
                this.CenterBoth();

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, () =>
                {
                    Cleanup();
                }, this);

            });

        }
        public Promise Play()
        {
            introDeferred = Deferred.Create();
            SpaceTime.QueueAction(PlaySceneInternal);
            SpaceTime.Start();
            return introDeferred.Promise;
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
                    character.Speed.SpeedX = -8;

                    // drop a timed mine
                    var dropper = new TimedMineDropper() { Delay = TimeSpan.FromSeconds(4), AmmoAmount = 1, Holder = character };
                    dropper.Exploded.SubscribeOnce(() => Sound.Play("PowerArgsIntro"));
                    dropper.FireInternal();

                    // eventually he will hit the left wall, remove him when that happens
                    character.Speed.ImpactOccurred.SubscribeForLifetime((i) => character.Lifetime.Dispose(), character.Lifetime);

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
                    Cleanup();
                }
            }));
        }

        public void Cleanup()
        {
            if (SpaceTime.IsRunning == false)
            {
                return;
            }

            SpaceTime.QueueAction(() =>
            {
                if (introDeferred.IsFulfilled)
                {
                    return;
                }

                SpaceTime.Elements.ToList().ForEach(e => e.Lifetime.Dispose());
                SpaceTime.Stop()
                .Then(introDeferred.Resolve)
                .Fail((ex => introDeferred.Reject(ex)))
                .Finally((p) => this.Dispose());
            });
        }
    }

    public class LetterReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
        {
            if (item.FG != ConsoleColor.Red)
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

    public class FlammableLetter : SpacialElement
    {
        public ConsoleCharacter Symbol { get; set; }

        public override void Initialize()
        {

        }

        public override void Evaluate()
        {
            Fire.BurnIfTouchingSomethingHot(this, TimeSpan.FromSeconds(4), this.Symbol.Value, true);
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
