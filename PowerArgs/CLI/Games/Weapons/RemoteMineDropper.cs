using PowerArgs.Cli.Physics;
using System.Linq;
namespace PowerArgs.Games
{
    public class RemoteMineDropper : Weapon
    {
        public override WeaponStyle Style => WeaponStyle.Explosive;

        public override void FireInternal(bool alt)
        {
            var ex = new Explosive(this);
            ex.SetProperty<Character>(nameof(Holder), this.Holder);
            ProximityMineDropper.PlaceMineSafe(ex, Holder, !alt);
            SpaceTime.CurrentSpaceTime.Add(ex);
            OnWeaponElementEmitted.Fire(ex);
        }

        public static bool Any(Character holder) => SpaceTime.CurrentSpaceTime.Elements
                .WhereAs<Explosive>()
                .Where(e => e.GetProperty<Character>(nameof(Holder)) == holder)
                .Any();
        

        public static void DetonateAll(Character holder, float delay = 250)
        {
            var mines = SpaceTime.CurrentSpaceTime.Elements
            .WhereAs<Explosive>()
            .Where(e => e.GetProperty<Character>(nameof(Holder)) == holder)
            .ToList();
            SpaceTime.CurrentSpaceTime.Invoke(async() =>
            {
                foreach(var mine in mines)
                {
                    if(mine.Lifetime.IsExpired == false)
                    {
                        mine.Explode();
                        await Time.CurrentTime.DelayAsync(delay);
                    }
                }
            });
        }
    }
}
