using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleGames
{

    public class Fire : SpacialElement
    {
        private TimeSpan duration;
        private TimeSpan initTime;

        private static Promise<IDisposable> currentSound;

        public Fire(TimeSpan duration)
        {
            this.duration = duration;
            this.Governor.Rate = TimeSpan.FromSeconds(.25);
            this.ResizeTo(1, 1);
            this.Tags.Add("hot");
        }

        public static void BurnIfTouchingSomethingHot<T>(T me) where T : SpacialElement, IDestructible
        {
            if (SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasTag("hot") && e.CalculateDistanceTo(me) < 2).Count() > 0)
            {
                var fire = new Fire(TimeSpan.FromSeconds(3));
                fire.MoveTo(me.Left, me.Top, me.ZIndex+1);
                fire.ResizeTo(me.Width, me.Height);
                SpaceTime.CurrentSpaceTime.Add(fire);
            }
        }

        public override void Initialize()
        {
            initTime = Time.CurrentTime.Now;
            if(currentSound == null)
            {
                currentSound = Sound.Loop("burn");
            }

            this.Lifetime.OnDisposed(() =>
            {
                if(currentSound != null && SpaceTime.CurrentSpaceTime.Elements.Where(e => e != this && e is Fire).Count() == 0)
                {
                    currentSound?.Result.Dispose();
                    currentSound = null;
                }
            });
        }

        public override void Evaluate()
        {
            if(Time.CurrentTime.Now - initTime >= duration)
            {
                this.Lifetime.Dispose();
                return;
            }

            SpaceTime.CurrentSpaceTime.Elements
                .Where(e => e is IDestructible && this.Touches(e))
                .Select(e => e as IDestructible)
                .ForEach(d => d.TakeDamage(1));

       
        }


    }

    [SpacialElementBinding(typeof(Fire))]
    public class FireRenderer : ThemeAwareSpacialElementRenderer
    {
        public ConsoleColor PrimaryBurnColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor SecondaryBurnColor { get; set; } = ConsoleColor.Red;

        public char BurnSymbol1 { get; set; } = '~';
        public char BurnSymbol2 { get; set; } = '-';

        private static Random r = new Random();

        public FireRenderer()
        {
            this.TransparentBackground = true;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if (r.NextDouble() < .8)
            {
                var color = r.NextDouble() < .8 ? PrimaryBurnColor : SecondaryBurnColor;
                var symbol = r.NextDouble() < .5 ? BurnSymbol1 : BurnSymbol2;

                context.Pen = new ConsoleCharacter(symbol, color);
                context.FillRect(0, 0, Width, Height);
            }
        }
    }
}
