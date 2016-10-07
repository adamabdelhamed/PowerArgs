using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class RemoteMine : Explosive
    {
        public RemoteMine(Rectangle bounds, float angleIcrement, float range) : base(bounds, angleIcrement, range)
        {

        }

        public void Detonate()
        {
            Explode();
        }
    }
}
