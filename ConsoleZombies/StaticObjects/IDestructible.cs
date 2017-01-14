using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public interface IDestructible
    {
        float HealthPoints { get; set; }
    }

    public static class IDestructibleEx
    {
        public static void TakeDamage(this IDestructible destructible, float damage)
        {
            if (destructible.HealthPoints > 0)
            {
                destructible.HealthPoints -= damage;
                if (destructible.HealthPoints <= 0)
                {
                    if (destructible is MainCharacter)
                    {
                        MainCharacter.Current.EatenByZombie.Fire();
                    }
                    (destructible as Thing).Scene.Remove(destructible as Thing);
                }
            }
        }
    }
}
