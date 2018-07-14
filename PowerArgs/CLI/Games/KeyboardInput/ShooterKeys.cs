using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace PowerArgs.Games
{
    public class ShooterKeys : ObservableObject
    {
        public ConsoleKey PrimaryWeaponKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey ExplosiveWeaponKey { get => Get<ConsoleKey>(); set => Set(value); }

        public ConsoleKey MoveUpKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey MoveDownKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey MoveLeftKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey MoveRightKey { get => Get<ConsoleKey>(); set => Set(value); }



        public ConsoleKey AimToggleKey { get => Get<ConsoleKey>(); set => Set(value); }

        public ConsoleKey TogglePauseKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey MenuKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey InteractKey { get => Get<ConsoleKey>(); set => Set(value); }

        public ConsoleKey PistolKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey ShotgunKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey NetKey { get => Get<ConsoleKey>(); set => Set(value); }

        public ConsoleKey RPGLauncherKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey RemoteMineKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey TimedMineKey { get => Get<ConsoleKey>(); set => Set(value); }
        public ConsoleKey ProximityMineKey { get => Get<ConsoleKey>(); set => Set(value); }

        private Func<SpaceTime> spaceTimeResolver;

        public ShooterKeys(Func<SpaceTime> spaceTimeResolver)
        {
            this.spaceTimeResolver = spaceTimeResolver;
            this.PrimaryWeaponKey = ConsoleKey.H;
            this.ExplosiveWeaponKey = ConsoleKey.G;

            this.MoveUpKey = ConsoleKey.W;
            this.MoveDownKey = ConsoleKey.S;
            this.MoveLeftKey = ConsoleKey.A;
            this.MoveRightKey = ConsoleKey.D;

            this.AimToggleKey = ConsoleKey.Q;
            this.InteractKey = ConsoleKey.Enter;

            this.MenuKey = ConsoleKey.M;
            this.TogglePauseKey = ConsoleKey.P;

            this.PistolKey = ConsoleKey.D1;
            this.ShotgunKey = ConsoleKey.D2;
            this.NetKey = ConsoleKey.D3;

            this.RPGLauncherKey = ConsoleKey.D5;
            this.RemoteMineKey = ConsoleKey.D6;
            this.TimedMineKey = ConsoleKey.D7;
            this.ProximityMineKey = ConsoleKey.D8;
        }

        public KeyMap ToKeyMap()
        {
            var ret = new KeyMap();

            ret.KeyboardMap.Add(MoveUpKey, ( )=> { MainCharacter.Current?.MoveUp(); });
            ret.KeyboardMap.Add(MoveDownKey, () => MainCharacter.Current?.MoveDown());
            ret.KeyboardMap.Add(MoveLeftKey, () => MainCharacter.Current?.MoveLeft());
            ret.KeyboardMap.Add(MoveRightKey, () => { MainCharacter.Current?.MoveRight(); });

            ret.KeyboardMap.Add(AimToggleKey, () => MainCharacter.Current?.ToggleFreeAim());
            ret.KeyboardMap.Add(InteractKey, () => MainCharacter.Current?.TryInteract());
            ret.KeyboardMap.Add(PrimaryWeaponKey, () => (MainCharacter.Current?.Inventory)?.PrimaryWeapon?.TryFire());
            ret.KeyboardMap.Add(ExplosiveWeaponKey, () => (MainCharacter.Current?.Inventory)?.ExplosiveWeapon?.TryFire());
            ret.KeyboardMap.Add(TogglePauseKey, () => (SpaceTime.CurrentSpaceTime.Application as GameApp)?.Pause(true));
            ret.KeyboardMap.Add(MenuKey, () => ShowKeyMapForm());

            ret.KeyboardMap.Add(PistolKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(Pistol)));
            ret.KeyboardMap.Add(ShotgunKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(Shotgun)));
            ret.KeyboardMap.Add(NetKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(Net)));
            ret.KeyboardMap.Add(RPGLauncherKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(RPGLauncher)));
            ret.KeyboardMap.Add(RemoteMineKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(RemoteMineDropper)));
            ret.KeyboardMap.Add(TimedMineKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(TimedMineDropper)));
            ret.KeyboardMap.Add(ProximityMineKey, () => MainCharacter.Current?.Inventory.TryEquip(typeof(ProximityMineDropper)));

            foreach(var key in ret.KeyboardMap.Keys.ToList())
            {
                var rawAction = ret.KeyboardMap[key];
                ret.KeyboardMap[key] = () =>
                {
                    spaceTimeResolver()?.QueueAction(rawAction);
                };
            }

            return ret;
        }

        private void ShowKeyMapForm()
        {
            var spaceTime = SpaceTime.CurrentSpaceTime;
            var game = (spaceTime.Application as GameApp);
            game.Pause(false);
            game.QueueAction(() => 
            {
                var dialog = new Dialog(new Form(FormOptions.FromObject(this)));
                dialog.Show().Then(() =>
                {
                    game.KeyboardInput.KeyMap = this.ToKeyMap();
                    game.Resume();
                });
            });
        }
    }
}
