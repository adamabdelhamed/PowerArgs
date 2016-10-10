using PowerArgs.Cli;

namespace ConsoleZombies
{
    public abstract class Weapon : ObservableObject
    {
        public int AmmoAmount { get { return Get<int>(); } set
            {
                Set(value);
            } }

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
