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
            Server.Broadcast(new StartGameMessage());
        }

        private void StartListeningForPlayerMovement()
        {
            Server.MessageRouter.Register(nameof(BoundsMessage), (message) =>
            {
                Server.Broadcast(message.Data);
            }
            , this);
        }

        private void StartListeningForPlayerFiring()
        {
            Server.MessageRouter.Register(nameof(RPGFireMessage), (message) =>
            {
                Server.Broadcast(message.Data);
            }
          , this);
        }

        private void StartListeningForDamageRequests()
        {
            Server.MessageRouter.Register(nameof(DamageMessage), (args) =>
            {
                var message = args.Data as DamageMessage;
    
                playerHealthPoints[message.DamagedClient] = message.NewHP;
                if(playerHealthPoints[message.DamagedClient] <= 0)
                {
                    playerHealthPoints.Remove(message.DamagedClient);
                    Server.Broadcast(new DeadMessage() { DeadClient = message.DamagedClient });

                    if(playerHealthPoints.Count == 1)
                    {
                        Server.Broadcast(new GameOverMessage() { Winner = playerHealthPoints.First().Key });
                        this.Dispose();
                    }
                }
                else
                {
                    Server.Broadcast(new NewHPMessage() { NewHP = message.NewHP, ClientWithNewHP = message.DamagedClient });
                }

                Server.Respond(new Ack() { RequestId = args.Data.RequestId, Recipient = args.Data.Sender });
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
        public string Winner { get; set; }

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
