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
            Router.Register(nameof(RPGFireMessage), RemoteFireRPG, this.Lifetime);
        }

        private void ReportDamageToServer()
        {
            MultiPlayerClient.SendRequest(new DamageMessage()
            {
                DamagedClient = this.remoteClientId,
                NewHP = this.HealthPoints
            });
        }

        private void RemoteFireRPG(RoutedEvent<MultiPlayerMessage> args)
        {
            if (Inventory.ExplosiveWeapon is RPGLauncher)
            {
                this.spaceTime.QueueAction(() => (Inventory.ExplosiveWeapon as IMultiPlayerWeapon).RemoteFire(args.Data));
            }
        }
    }

    [SpacialElementBinding(typeof(RemoteCharacter))]
    public class RemoteCharacterRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('R', ConsoleColor.Red);
    }
}
