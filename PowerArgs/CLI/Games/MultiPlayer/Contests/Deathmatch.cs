using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Games
{

    public class Deathmatch : MultiPlayerContest<MultiPlayerContestOptions>
    {
        public Event<Exception> OrchestrationFailed { get; private set; } = new Event<Exception>();
        private Dictionary<string, float> playerHealthPoints = new Dictionary<string, float>();

        public Deathmatch(MultiPlayerContestOptions options) : base(options) { }

        public Task Start()
        {
            StartGameInternal();
            return Options.Server.OpenForNewConnections();
        }
       

        private async void StartGameInternal()
        {
            try
            {
                await WaitUntilFull();
                InitializeHealthPoints();
                NotifyPlayersOfGameStart();
                StartListeningForPlayerMovement();
                StartListeningForPlayerFiring();
                StartListeningForDamageRequests();
            }
            catch (Exception ex)
            {
                OrchestrationFailed.Fire(ex);
            }
        }
   
        private async Task WaitUntilFull()
        {
            var lobbyLifetime = new Lifetime();
            this.Options.Server.Connections.Changed.SubscribeForLifetime(() =>
            {
                if(Options.Server.Connections.Count == Options.MaxPlayers)
                {
                    lobbyLifetime.Dispose();
                }
            }, lobbyLifetime);
          
            await lobbyLifetime.AwaitEndOfLifetime();
            await Options.Server.CloseForNewConnections();
        }

        private void InitializeHealthPoints()
        {
            playerHealthPoints.Clear();
            foreach (var client in this.Options.Server.Connections)
            {
                playerHealthPoints.Add(client.ClientId, 100);
            }
        }

        private void NotifyPlayersOfGameStart()
        {
            var i = 0;
            Server.TryBroadcast((connection) =>
            {
                i++;
                return new BoundsMessage()
                {
                    X = 5 + (i * 25),
                    Y = 5,
                    W = 1,
                    H = 1,
                    Recipient = connection.ClientId,
                    ClientToUpdate = connection.ClientId
                };
            });

            Server.TryBroadcast((connection) => new StartGameMessage() { Recipient = connection.ClientId });
        }

        private void StartListeningForPlayerMovement()
        {
            Server.MessageRouter.Register<BoundsMessage>((message) =>
            {
                Server.Info.Fire($"{message.Sender} sent new position and velocity");
                Server.TryBroadcast((connection) => message.Sender == connection.ClientId ? null : message);
            }, this);
        }

        private void StartListeningForPlayerFiring()
        {
            Server.MessageRouter.Register<RPGFireMessage>((message) =>
            {
                Server.Info.Fire($"{message.Sender} fired an RPG");
                Server.TryBroadcast((connection) => message.Sender == connection.ClientId ? null : message);
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
                    Server.Broadcast((connection) => new DeadMessage() { DeadClient = message.DamagedClient });

                    if(playerHealthPoints.Count == 1)
                    {
                        var info = Server.Connections.Where(c => c.ClientId == playerHealthPoints.First().Key).Single();

                        Server.TryBroadcast((connection) => new GameOverMessage() { WinnerId = info.ClientId, WinnerDisplayName = info.DisplayName });
                        this.Dispose();
                    }
                }
                else
                {
                    Server.TryBroadcast((connection)=>new NewHPMessage() { NewHP = message.NewHP, ClientWithNewHP = message.DamagedClient });
                }

                Server.TryRespond(new Ack() { RequestId = message.RequestId, Recipient = message.Sender });
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
        public float Speed { get; set; }
        public float Angle { get; set; }
    }
}
