using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Games
{
    public class DamageEventArgs
    {
        public SpacialElement Damager { get; set; }
        public SpacialElement Damagee { get; set; }
    }

    public interface IDamageEnforcer
    {
        void EnforceDamage(DamageEventArgs args);
    }

    public class DamageBroker
    {
        public const string DamageableTag = "damageable";
        private static Lazy<DamageBroker> instance = new Lazy<DamageBroker>(()=> new DamageBroker());
        public static DamageBroker Instance => instance.Value;
        private DamageBroker() { }
        public IDamageEnforcer DamageEnforcer { get; set; }
        public void ReportDamage(DamageEventArgs args) => DamageEnforcer?.EnforceDamage(args);

        public void ReportImpact(Impact impact)
        {
            if(IsDamageable(impact.ObstacleHit))
            {
                ReportDamage(new DamageEventArgs()
                {
                    Damager = impact.MovingObject,
                    Damagee = impact.ObstacleHit as SpacialElement
                });
            }
        }

        public bool IsDamageable(IRectangular el) => el is SpacialElement && (el as SpacialElement).HasSimpleTag(DamageableTag);

        public IEnumerable<SpacialElement> DamageableElements =
            SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag(DamageableTag));
    }
}
