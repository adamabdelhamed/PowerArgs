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
        private Dictionary<string, int> playerHealthPoints = new Dictionary<string, int>();

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
            Server.MessageRouter.RegisterRouteForLifetime("bounds/{*}", (message) =>
            {
                Server.Broadcast(message.Data);
            }
            , this);
        }

        private void StartListeningForDamageRequests()
        {
            Server.MessageRouter.RegisterRouteForLifetime("damage/{*}", (args) =>
            {
                var clientIdOfDamagedPlayer = args.Data.Data["OpponentId"];

                playerHealthPoints[clientIdOfDamagedPlayer] = playerHealthPoints[clientIdOfDamagedPlayer] - 10;
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

                Server.Respond(args.Data, new Dictionary<string, string>()
                {
                    {"accepted", "true" }
                });
            }, this);
        }
    }
}
