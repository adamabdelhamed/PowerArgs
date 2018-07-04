using PowerArgs.Cli.Physics;

namespace ConsoleGames
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
                activeMine = new Explosive();
                activeMine.MoveTo(Holder.Left, Holder.Top);
                SpaceTime.CurrentSpaceTime.Add(activeMine);
            }
        }
    }
}
