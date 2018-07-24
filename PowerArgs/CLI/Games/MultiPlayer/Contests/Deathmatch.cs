using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace PowerArgs.Games
{

    public class Deathmatch : MultiPlayerContest<MultiPlayerContestOptions>
    {
        private Dictionary<string, float> playerHealthPoints = new Dictionary<string, float>();

        public Deathmatch(MultiPlayerContestOptions options) : base(options)
        {
 
        }

        public override void Start() => OrchestrateGameAsync();

        private async void OrchestrateGameAsync()
        {
            await WaitUntilFull();
            InitializeHealthPoints();
            NotifyPlayersOfGameStart();
            StartListeningForPlayerMovement();
            StartListeningForPlayerFiring();
            StartListeningForDamageRequests();
        }

        private async Task WaitUntilFull()
        {
            var lobbyLifetime = new Lifetime();
            this.Options.Server.clients.Changed.SubscribeForLifetime(() =>
            {
                if(Options.Server.clients.Count == Options.MaxPlayers)
                {
                    lobbyLifetime.Dispose();
                }
            }, lobbyLifetime);
            await Options.Server.OpenForNewConnections().AsAwaitable();
            await lobbyLifetime.AwaitEndOfLifetime();
            await Options.Server.CloseForNewConnections().AsAwaitable();
        }

        private void InitializeHealthPoints()
        {
            playerHealthPoints.Clear();
            foreach (var client in this.Options.Server.clients)
            {
                playerHealthPoints.Add(client.ClientId, 100);
            }
        }

        private void NotifyPlayersOfGameStart()
        {
            Server.Broadcast(MultiPlayerMessage.Create(ServerId, null, "start"));
        }

        private void StartListeningForPlayerMovement()
        {
            Server.MessageRouter.Register("bounds/{*}", (message) =>
            {
                Server.Broadcast(message.Data);
            }
            , this);
        }

        private void StartListeningForPlayerFiring()
        {
            Server.MessageRouter.Register("fireprimary/{*}", (message) =>
            {
                Server.Broadcast(message.Data);
            }
            , this);

            Server.MessageRouter.Register("fireexplosive/{*}", (message) =>
            {
                Server.Broadcast(message.Data);
            }
          , this);
        }

        private void StartListeningForDamageRequests()
        {
            Server.MessageRouter.Register("damage/{*}", (args) =>
            {
                var clientIdOfDamagedPlayer = args.Data.Data["ClientId"];
                var newHp = float.Parse(args.Data.Data["NewHP"]);
                playerHealthPoints[clientIdOfDamagedPlayer] = newHp;
                if(playerHealthPoints[clientIdOfDamagedPlayer] <= 0)
                {
                    playerHealthPoints.Remove(clientIdOfDamagedPlayer);
                    Server.Broadcast(MultiPlayerMessage.Create(ServerId, null, "dead", new Dictionary<string, string>()
                    {
                        { "ClientId", clientIdOfDamagedPlayer }
                    }));

                    if(playerHealthPoints.Count == 1)
                    {
                        Server.Broadcast(MultiPlayerMessage.Create(ServerId, null, "gameover", new Dictionary<string, string>()
                        {
                            { "winner", playerHealthPoints.First().Key }
                        }));
                        this.Dispose();
                    }
                }
                else
                {
                    Server.Broadcast(MultiPlayerMessage.Create(ServerId, null, "NewHP", new Dictionary<string, string>()
                    {
                        { "ClientId", clientIdOfDamagedPlayer },
                        { "NewHP", newHp+"" }
                    }));
                }

                Server.Respond(args.Data, new Dictionary<string, string>()
                {
                    {"accepted", "true" }
                });
            }, this);
        }
    }
}
