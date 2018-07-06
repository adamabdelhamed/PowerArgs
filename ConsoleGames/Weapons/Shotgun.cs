using PowerArgs.Cli.Physics;
namespace ConsoleGames
{
    public class Shotgun : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;
        public float HealthPoints { get; set; } = 10; 

        public override void FireInternal()
        {
            var targetAngle = CalculateAngleToTarget();
            var sprayAngle = SpaceExtensions.NormalizeQuantity(30.0f, targetAngle, true);
            var sprayIncrement = 5;
            var startAngle = SpaceExtensions.AddToAngle(targetAngle,-sprayAngle/2);
            var sprayedSoFar = 0;

            Sound.Play("pistol");

            while (sprayedSoFar < sprayAngle)
            {
                sprayedSoFar += sprayIncrement;
                var angle = SpaceExtensions.AddToAngle(startAngle, sprayedSoFar);
                var bullet = new Projectile(Holder.Left, Holder.Top, angle) { Range = SpaceExtensions.NormalizeQuantity(8, angle), HealthPoints = HealthPoints, PlaySoundOnImpact = true };
                bullet.Speed.HitDetectionTypes.Remove(Holder.GetType());

                if (Holder.Target != null)
                {
                    bullet.Speed.HitDetectionTypes.Add(Holder.Target.GetType());
                }
                SpaceTime.CurrentSpaceTime.Add(bullet);
            }
        }
    }
}
