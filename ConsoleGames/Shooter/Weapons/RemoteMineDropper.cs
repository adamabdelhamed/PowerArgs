using PowerArgs.Cli.Physics;

namespace ConsoleGames.Shooter
{
    public class RemoteMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        RemoteMine activeMine;
        public override void FireInternal()
        {
            if (activeMine != null)
            {
                activeMine.Detonate();
                activeMine = null;
            }
            else
            {
                activeMine = new RemoteMine(MainCharacter.Current.Left, MainCharacter.Current.Top, 5, 4) { HealthPointsPerShrapnel = 5 };
                SpaceTime.CurrentSpaceTime.Add(activeMine);
            }
        }
    }
}
