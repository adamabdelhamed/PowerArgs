using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class RemoteCharacter : Character
    {
        private EventRouter<MultiPlayerMessage> Router => this.MultiPlayerClient.EventRouter;
        private string remoteClientId;
        private SpaceTime spaceTime;

        public RemoteCharacter(MultiPlayerClient client, string remoteId)
        {
            this.remoteClientId = remoteId;
            this.Added.SubscribeOnce(() => { this.spaceTime = SpaceTime.CurrentSpaceTime; });
            this.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
            this.MultiPlayerClient = client;

            this.Damaged.SubscribeForLifetime(ReportDamageToServer, this.Lifetime);
            Router.Register("fireprimary/{*}", RemoteFire, this.Lifetime);
            Router.Register("fireexplosive/{*}", RemoteFire, this.Lifetime);
        }

        private void ReportDamageToServer()
        {
            MultiPlayerClient.SendRequest(MultiPlayerMessage.Create(MultiPlayerClient.ClientId, null, "damage", new Dictionary<string, string>()
            {
                { "ClientId", this.remoteClientId },
                { "NewHP", this.HealthPoints+"" }
            }));
        }

        private void RemoteFire(RoutedEvent<MultiPlayerMessage> args)
        {
            IMultiPlayerWeapon toFire = args.Data.EventId == "fireprimary"  ?
                this.Inventory.PrimaryWeapon as IMultiPlayerWeapon :
                this.Inventory.ExplosiveWeapon as IMultiPlayerWeapon;

            if (toFire != null)
            {
                this.spaceTime.QueueAction(() => toFire.RemoteFire(args.Data));
            }
        }
    }

    [SpacialElementBinding(typeof(RemoteCharacter))]
    public class RemoteCharacterRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('R', ConsoleColor.Red);
    }
}
