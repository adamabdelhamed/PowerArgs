using PowerArgs;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace ConsoleGames
{
    public class Sword : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public int Range { get; set; } = 7;

        private List<Blade> activeBlades = new List<Blade>();

        public override void FireInternal()
        {
            activeBlades.ForEach(b => { if (b.Lifetime.IsExpired == false) b.Lifetime.Dispose(); });
            activeBlades.Clear();

            Sound.Play("sword");
            for(var i = 1; i < 1+ SpaceExtensions.NormalizeQuantity(Range, CalculateAngleToTarget()); i++)
            {
                var location = Holder.Center().MoveTowards(CalculateAngleToTarget(), i);
                var newBounds = Rectangular.Create(location.Left - .5f, location.Top - .5f, 1, 1);
                if (SpaceTime.CurrentSpaceTime.IsInBounds(newBounds))
                {
                    var blade = new Blade() { Holder = this.Holder };
                    var holderLocation = Holder.TopLeft();
                    blade.MoveTo(newBounds.Left, newBounds.Top);
                    SpaceTime.CurrentSpaceTime.Add(blade);
                    activeBlades.Add(blade);
                }
            }
        }
    }

    public class Blade : SpacialElement
    {
        public Character Holder { get; set; }
        public float HealthPoints { get; set; } = 1;

        private float dx;
        private float dy;

        public override void Initialize()
        {
            dx = Holder.Left - this.Left;
            dy = Holder.Top - this.Top;
        }

        public override void Evaluate()
        {
            if(this.CalculateAge().TotalSeconds >= .3)
            {
                this.Lifetime.Dispose();
            }

            this.MoveTo(Holder.Left + dx, Holder.Top + dy);

            SpaceTime.CurrentSpaceTime.Elements
                .Where(e => e != Holder && e.Touches(this))
                .WhereAs<IDestructible>()
                .ForEach(d => d.TakeDamage(HealthPoints));
        }
    }

    [SpacialElementBinding(typeof(Blade))]
    public class BladeRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('=', ConsoleColor.Cyan);
    }
}
