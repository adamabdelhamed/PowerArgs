using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class RemoteCharacter : Character
    {
        private ObservableObject observable = new ObservableObject();
        private MultiPlayerMessageRouter Router => this.MultiPlayerClient.EventRouter;
        public string RemoteClientId { get; private set; }
        public bool IsConnected { get => observable.Get<bool>(); set => observable.Set(value); }

        private SpaceTime spaceTime;

        public RemoteCharacter(MultiPlayerClient client, string remoteId)
        {
            this.RemoteClientId = remoteId;
            this.Added.SubscribeOnce(() => { this.spaceTime = SpaceTime.CurrentSpaceTime; });
            this.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
            this.MultiPlayerClient = client;

            this.Damaged.SubscribeForLifetime(ReportDamageToServer, this.Lifetime);
            Router.Register<RPGFireMessage>(RemoteFireRPG, this.Lifetime);
            IsConnected = true;
            observable.SynchronizeForLifetime(nameof(IsConnected), ()=> this.SizeOrPositionChanged.Fire(), this.Lifetime);
        }

        private void ReportDamageToServer()
        {
            MultiPlayerClient.SendRequest(new DamageMessage()
            {
                DamagedClient = this.RemoteClientId,
                NewHP = this.HealthPoints
            });
        }

        private void RemoteFireRPG(RPGFireMessage message)
        {
            if (Inventory.ExplosiveWeapon is RPGLauncher)
            {
                this.spaceTime.QueueAction(() => (Inventory.ExplosiveWeapon as IMultiPlayerWeapon).RemoteFire(message));
            }
        }
    }

    [SpacialElementBinding(typeof(RemoteCharacter))]
    public class RemoteCharacterRenderer : SingleStyleRenderer
    {
        protected override ConsoleCharacter DefaultStyle => new ConsoleCharacter('R', ConsoleColor.White);

        public override void OnRender()
        {
            Style = (Element as RemoteCharacter).IsConnected ? new ConsoleCharacter('R', ConsoleColor.Cyan) : new ConsoleCharacter('R', ConsoleColor.DarkGray);
        }
    }
}
