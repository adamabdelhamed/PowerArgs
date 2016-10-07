using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public abstract class Item : Thing
    {
        public abstract void IncorporateInto(Inventory inventory);

        public override void Behave(Scene r)
        {
            if(MainCharacter.Current != null)
            {
                if(MainCharacter.Current.Bounds.Hits(this.Bounds))
                {
                    IncorporateInto(MainCharacter.Current.Inventory);
                    Scene.Remove(this);
                }
            }
        }
    }

}
