using PowerArgs.Cli.Physics;
using System;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    /*
            Orbit still has a bug where the satellite sometimes doesn't render in the right place.
            It's like there's a rounding bug somewhere. // TODO - go back and fix it
    */

    public class Orbit : SpacialElementFunction
    {
        public class Satellite : SpacialElement
        {
            public bool IsShadow { get; set; }
            public char Symbol { get; set; }
        }

        [SpacialElementBinding(typeof(Satellite))]
        public class SatelliteRenderer : SpacialElementRenderer
        {
            protected override void OnPaint(ConsoleBitmap context)
            {
                context.Pen = new ConsoleCharacter((Element as Satellite).Symbol, (Element as Satellite).IsShadow ? ConsoleColor.DarkGray : ConsoleColor.Green);
                context.FillRect(0, 0, Width, Height);
            }
        }
        private char[] chars = new char[] { '*', '*', '*', '*', '*', '*', '*', '*' };
        //private char[] chars = new char[] { '|', '/', '-', '\\', '|', '/', '-', '\\' };
        private float[] xOffsets = new float[] { 0, 1, 2, 1, 0, -1, - 2, - 1 };
        private float[] yOffsets = new float[] { -1, -1, 0, 1, 1, 1, 0, -1 };
        private int currentIndex = 0;

        private float SatelliteLeft => Element.Left + xOffsets[currentIndex];
        private float SatelliteTop => Element.Top + yOffsets[currentIndex];
        public char SatelliteSymbol => chars[currentIndex];

        private int shadowIndex => currentIndex == 0 ? chars.Length - 1 : currentIndex - 1;
        private float ShadowLeft => Element.Left + xOffsets[shadowIndex];
        private float ShadowTop => Element.Top + yOffsets[shadowIndex];
        public char ShadowSymbol => chars[shadowIndex];

        private Satellite satellite;
        private Satellite shadow;

        private RateGovernor animateCharacterGovernor = new RateGovernor(TimeSpan.FromMilliseconds(75));
        public Orbit(SpacialElement toOrbit) : base(toOrbit) { }

        public override void Initialize()
        {
            if(Element.Width != 1 || Element.Height != 1)
            {
                throw new NotSupportedException("Target element must be 1 x 1");
            }

            satellite = SpaceTime.CurrentSpaceTime.Add(new Satellite());
            shadow = SpaceTime.CurrentSpaceTime.Add(new Satellite() { IsShadow = true });
            this.Lifetime.OnDisposed(satellite.Lifetime.Dispose);
            this.Lifetime.OnDisposed(shadow.Lifetime.Dispose);
            Element.SizeOrPositionChanged.SubscribeForLifetime(()=>
            {
                satellite.MoveTo(SatelliteLeft, SatelliteTop);
                shadow.MoveTo(ShadowLeft, ShadowTop);
            }, this.Lifetime);
            Element.SizeOrPositionChanged.Fire();
        }
        
        public override void Evaluate()
        {
            if (Element.Width != 1 || Element.Height != 1)
            {
                throw new NotSupportedException("Target element must be 1 x 1");
            }
            satellite.MoveTo(SatelliteLeft, SatelliteTop);
            satellite.Symbol = SatelliteSymbol;

            shadow.MoveTo(ShadowLeft, ShadowTop);
            shadow.Symbol = ShadowSymbol;

            if (animateCharacterGovernor.ShouldFire(Time.CurrentTime.Now))
            {
                currentIndex = currentIndex + 1 < chars.Length ? currentIndex + 1 : 0;
            }
        }
    }
}
