using Newtonsoft.Json;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public interface ISerializableThing
    {
        Rectangle Bounds { get; set; }
        [JsonIgnore]
        Thing HydratedThing { get; }

        [JsonIgnore]
        int RehydrateOrderHint { get; set; }
        void Rehydrate(bool isInLevelBuilder);
    }
}
