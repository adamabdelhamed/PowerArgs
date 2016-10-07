namespace ConsoleZombies
{
    public class Inventory
    {
        public Weapon Gun { get; private set; }
        public Weapon RemoteMineDropper { get; private set; }
        public Weapon TimedMineDropper { get; private set; }
        public Weapon RPGLauncher { get; private set; }
        public Inventory()
        {
            Gun = new Pistol() { AmmoAmount = 20 };
            RemoteMineDropper = new RemoteMineDropper() { AmmoAmount = 2 } ;
            TimedMineDropper = new TimedMineDropper() { AmmoAmount = 2 };
            RPGLauncher = new RPGLauncher() { AmmoAmount = 2 };
        }
    }
}
