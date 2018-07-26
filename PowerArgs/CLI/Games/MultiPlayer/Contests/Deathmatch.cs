using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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
            this.Options.Server.Clients.Changed.SubscribeForLifetime(() =>
            {
                if(Options.Server.Clients.Count == Options.MaxPlayers)
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
            foreach (var client in this.Options.Server.Clients)
            {
                playerHealthPoints.Add(client.ClientId, 100);
            }
        }

        private void NotifyPlayersOfGameStart()
        {
            for(var i = 0; i < Server.Clients.Count; i++)
            {
                var client = Server.Clients[i];
                var x = 5 + (i * 25);
                var y = 5;
                Server.SendMessage(new BoundsMessage()
                {
                    X = x,
                    Y = y,
                    W = 1,
                    H = 1,
                    Recipient = client.ClientId,
                    ClientToUpdate = client.ClientId
                });
            }

            for (var i = 0; i < Server.Clients.Count; i++)
            {
                var client = Server.Clients[i];
                var x = 5 + (i * 25);
                var y = 5;
                Server.SendMessage(new StartGameMessage() { Recipient = client.ClientId });
            }
        }

        private void StartListeningForPlayerMovement()
        {
            Server.MessageRouter.Register< BoundsMessage>((message) =>
            {
                Server.Broadcast(message);
            } , this);
        }

        private void StartListeningForPlayerFiring()
        {
            Server.MessageRouter.Register<RPGFireMessage>((message) =>
            {
                Server.Info.Fire($"{message.Sender} fired an RPG");
                Server.Broadcast(message);
            }
          , this);
        }

        private void StartListeningForDamageRequests()
        {
            Server.MessageRouter.Register<DamageMessage>((message) =>
            {
                Server.Info.Fire($"Damage to {message.DamagedClient}");
                playerHealthPoints[message.DamagedClient] = message.NewHP;
                if(playerHealthPoints[message.DamagedClient] <= 0)
                {
                    playerHealthPoints.Remove(message.DamagedClient);
                    Server.Broadcast(new DeadMessage() { DeadClient = message.DamagedClient });

                    if(playerHealthPoints.Count == 1)
                    {
                        var info = Server.Clients.Where(c => c.ClientId == playerHealthPoints.First().Key).Single();

                        Server.Broadcast(new GameOverMessage() { WinnerId = info.ClientId, WinnerDisplayName = info.DisplayName });
                        this.Dispose();
                    }
                }
                else
                {
                    Server.Broadcast(new NewHPMessage() { NewHP = message.NewHP, ClientWithNewHP = message.DamagedClient });
                }

                Server.Respond(new Ack() { RequestId = message.RequestId, Recipient = message.Sender });
            }, this);
        }
    }

    public class DamageMessage : MultiPlayerMessage
    {
        public string DamagedClient { get; set; }
        public float NewHP { get; set; }
    }

    public class RPGFireMessage : MultiPlayerMessage
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }
    }

    public class NewHPMessage : MultiPlayerMessage
    {
        public string ClientWithNewHP { get; set; }
        public float NewHP { get; set; }
    }

    public class Ack : MultiPlayerMessage
    {
        public string Error { get; set; }
    }

    public class StartGameMessage : MultiPlayerMessage
    {

    }

    public class DeadMessage : MultiPlayerMessage
    {
        public string DeadClient { get; set; }
 
    }

    public class GameOverMessage : MultiPlayerMessage
    {
        public string WinnerId { get; set; }
        public string WinnerDisplayName { get; set; }

    }

    public class DamageResponse : MultiPlayerMessage
    {
        public bool Accepted { get; set; }
    }
 
    public class BoundsMessage : MultiPlayerMessage
    {
        public string ClientToUpdate { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float W { get; set; }
        public float H { get; set; }
    }
}
