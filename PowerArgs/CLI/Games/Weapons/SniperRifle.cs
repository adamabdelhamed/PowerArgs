using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class SniperRifle : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal()
        {
            Sound.Play("pistol");
            if (Holder.Target != null)
            {
                DamageBroker.Instance.ReportImpact(new Impact()
                {
                    HitType = HitType.Obstacle,
                    ObstacleHit = Holder.Target,
                    MovingObject= Holder,
                    Angle = Holder.CalculateAngleTo(Holder.Target)
                });
            }
            else
            {
                Sound.Play("miss");
            }
        }
    }
}
