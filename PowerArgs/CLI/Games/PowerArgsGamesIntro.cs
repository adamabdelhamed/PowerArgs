using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public class PowerArgsGamesIntro : SpaceTimePanel
    {
        private TaskCompletionSource<bool> introDeferred;
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
        public Task Play()
        {
            introDeferred = new TaskCompletionSource<bool>();
            SpaceTime.InvokeNextCycle(PlaySceneInternal);
            SpaceTime.Start();
            return introDeferred.Task;
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
            character.Velocity.Speed = 5;
            character.Velocity.Angle = 0;
            // he drops a timed mine and turns around when he gets near the right edge
            ListenForCharacterNearRightEdge();

            SpaceTime.Add(character);

            ListenForEndOfIntro();
         }

        private void ListenForCharacterNearRightEdge()
        {
            SpaceTime.Invoke(async () =>
            {
                while (SpaceTime.IsRunning)
                {
                    if (character.Left > Width - 2)
                    {
                        // turn the character around so he now moves to the left
                        character.Velocity.Speed = 8;
                        character.Velocity.Angle = 180;
                        // drop a timed mine
                        var dropper = new TimedMineDropper() { Delay = TimeSpan.FromSeconds(4), AmmoAmount = 1, Holder = character };
                        dropper.Exploded.SubscribeOnce(() => Sound.Play("PowerArgsIntro"));
                        dropper.FireInternal(false);

                        // eventually he will hit the left wall, remove him when that happens
                        character.Velocity.ImpactOccurred.SubscribeForLifetime((i) => character.Lifetime.Dispose(), character.Lifetime);

                        // this watcher has done its job, stop watching the secne 
                        break;
                    }
                    await SpaceTime.YieldAsync();
                }
            });
        
        }

        private void ListenForEndOfIntro()
        {
            SpaceTime.Invoke(async () =>
            {
                while (SpaceTime.IsRunning)
                {
                    var remainingCount = SpaceTime.Elements.Where(e => e is FlammableLetter || e is Fire).Count();
                    if (remainingCount == 0)
                    {
                        Cleanup();
                        await SpaceTime.YieldAsync();
                    }
                }
            });
        }

        public void Cleanup()
        {
            if (SpaceTime.IsRunning == false)
            {
                return;
            }

            SpaceTime.Invoke(async () =>
            {
                if (introDeferred.Task.IsFulfilled())
                {
                    return;
                }

                SpaceTime.Elements.ToList().ForEach(e => e.Lifetime.Dispose());
                introDeferred.SetResult(true);
                SpaceTime.Stop();
                this.Dispose();
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

        public FlammableLetter()
        {
            this.Added.SubscribeOnce(async () =>
            {
                while (this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.YieldAsync();
                }
            });
        }

        private void Evaluate()
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
