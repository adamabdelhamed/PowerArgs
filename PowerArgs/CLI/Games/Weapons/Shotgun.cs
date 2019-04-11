using PowerArgs.Cli.Physics;
namespace PowerArgs.Games
{
    public class Shotgun : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Primary;

        public override void FireInternal()
        {
            var targetAngle = CalculateAngleToTarget();
            var sprayAngle =  30.0f.NormalizeQuantity(targetAngle, true);
            var sprayIncrement = 5;
            var startAngle = targetAngle.AddToAngle(-sprayAngle/2);
            var sprayedSoFar = 0;

            Sound.Play("pistol");

            while (sprayedSoFar < sprayAngle)
            {
                sprayedSoFar += sprayIncrement;
                var angle = startAngle.AddToAngle(sprayedSoFar);
                var bullet = new Projectile(Holder.Left, Holder.Top, angle) { Range = 8.NormalizeQuantity(angle), PlaySoundOnImpact = true };
                SpaceTime.CurrentSpaceTime.Add(bullet);
            }
        }
    }
}
