using PowerArgs.Cli.Physics;

namespace PowerArgs.Games
{
    public class RemoteMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        Explosive activeMine;
        public override void FireInternal()
        {
            if (activeMine != null)
            {
                activeMine.Explode();
                activeMine = null;
            }
            else
            {
                activeMine = new Explosive(this);
                ProximityMineDropper.PlaceMineSafe(activeMine, Holder);
                SpaceTime.CurrentSpaceTime.Add(activeMine);
            }
        }
    }
}
