using Newtonsoft.Json;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
namespace ConsoleGames
{
    public enum WeaponStyle
    {
        Primary,
        Explosive
    }

    public abstract class Weapon : ObservableObject, IInventoryItem
    {
        [JsonIgnore]
        public Character Holder { get; set; }

        [JsonIgnore]
        public abstract WeaponStyle Style { get; }

        public int AmmoAmount
        {
            get { return Get<int>(); } set { Set(value); }
        }

        public float CalculateAngleToTarget()
        {
            var angle = Holder.Target != null ?
                Holder.CalculateAngleTo(Holder.Target) :
                MainCharacter.Current.Speed.Angle;

            if (Holder == MainCharacter.Current && MainCharacter.Current.FreeAimCursor != null)
            {
                angle = Holder.CalculateAngleTo(MainCharacter.Current.FreeAimCursor);
            };

            return angle;
        }

        public void TryFire()
        {
            if ((AmmoAmount > 0 || AmmoAmount == -1) && Holder != null)
            {
                FireInternal();
                if (AmmoAmount > 0)
                {
                    AmmoAmount--;
                }
            }
        }

        public abstract void FireInternal();
    }
}
