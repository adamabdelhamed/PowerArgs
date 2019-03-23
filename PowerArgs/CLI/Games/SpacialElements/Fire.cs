using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace PowerArgs.Games
{

    public class Fire : SpacialElement
    {
        private TimeSpan duration;
        private TimeSpan initTime;
        public char? SymbolOverride { get; set; }
        private static Promise<Lifetime> currentSound;

        public Fire(TimeSpan duration)
        {
            this.duration = duration;
            this.Governor.Rate = TimeSpan.FromSeconds(.1);
            this.ResizeTo(1, 1);
            this.Tags.Add("hot");
        }

        public static void BurnIfTouchingSomethingHot<T>(T me, TimeSpan? burnTime = null, char? symbol = null, bool disposeOnBurn = false) where T : SpacialElement
        {
            burnTime = burnTime.HasValue ? burnTime.Value : TimeSpan.FromSeconds(3);
            if (SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag("hot") && e.CalculateDistanceTo(me) < 2).Any())
            {
                if (SpaceTime.CurrentSpaceTime.Elements.WhereAs<Fire>().Where(f => f.Left == me.Left && f.Top == me.Top).Count() == 0)
                {
                    var fire = new Fire(burnTime.Value) { SymbolOverride = symbol };
                    fire.MoveTo(me.Left, me.Top, me.ZIndex + 1);
                    fire.ResizeTo(me.Width, me.Height);
                    SpaceTime.CurrentSpaceTime.Add(fire);
                    if (disposeOnBurn)
                    {
                        me.Lifetime.Dispose();
                    }
                }
            }
        }

        public override void Initialize()
        {
            initTime = Time.CurrentTime.Now;
            if (currentSound == null)
            {
                currentSound = Sound.Play("burn");
            }

            this.Lifetime.OnDisposed(() =>
            {
                if (currentSound != null && SpaceTime.CurrentSpaceTime.Elements.Where(e => e != this && e is Fire).Count() == 0)
                {
                    if (currentSound.Result.IsExpired == false)
                    {
                        currentSound?.Result.Dispose();
                    }
                    currentSound = null;
                }
            });
        }

        public override void Evaluate()
        {
            if (Time.CurrentTime.Now - initTime >= duration)
            {
                this.Lifetime.Dispose();
                return;
            }

            SpaceTime.CurrentSpaceTime.Elements
                .Where(e => e.HasSimpleTag("destructable") && this.Touches(e))
                .ForEach(d => DamageBroker.Instance.ReportDamage(new DamageEventArgs()
                {
                    Damager = this,
                    Damagee = d
                }));
            this.SizeOrPositionChanged.Fire();
        }


    }

    [SpacialElementBinding(typeof(Fire))]
    public class FireRenderer : SpacialElementRenderer
    {
        public ConsoleColor PrimaryBurnColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor SecondaryBurnColor { get; set; } = ConsoleColor.Red;

        public char BurnSymbol1 { get; set; } = '~';
        public char BurnSymbol2 { get; set; } = '-';

        private static Random r = new Random();
        private char symbol;
        private ConsoleColor color;
        public FireRenderer()
        {
            this.TransparentBackground = true;
        }

        public override void OnRender()
        {
            base.OnRender();
            var primarySymbol = (Element as Fire).SymbolOverride.HasValue ? (Element as Fire).SymbolOverride.Value : BurnSymbol1;
            if (primarySymbol != BurnSymbol1)
            {
                BurnSymbol2 = BurnSymbol1;
            }

            this.color = r.NextDouble() < .8 ? PrimaryBurnColor : SecondaryBurnColor;
            this.symbol = r.NextDouble() < .9 ? primarySymbol : BurnSymbol2;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.Pen = new ConsoleCharacter(symbol, color);
            context.FillRect(0, 0, Width, Height);
        }
    }
}
