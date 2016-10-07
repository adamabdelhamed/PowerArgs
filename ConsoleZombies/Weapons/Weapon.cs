namespace ConsoleZombies
{
    public abstract class Weapon
    {
        public int AmmoAmount { get; set; }

        public void TryFire()
        {
            if(AmmoAmount > 0)
            {
                FireInternal();
                AmmoAmount--;
            }
            else
            {

            }
        }

        public abstract void FireInternal();
    }
}
