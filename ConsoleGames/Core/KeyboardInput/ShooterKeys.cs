using PowerArgs.Cli;
using System;

namespace ConsoleGames
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

        public ShooterKeys()
        {
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
        }

        public KeyMap ToKeyMap()
        {
            var ret = new KeyMap();

            ret.KeyboardMap.Add(MoveUpKey, ( )=>
            {
                MainCharacter.Current?.MoveUp();
            });
            ret.KeyboardMap.Add(MoveDownKey, () => MainCharacter.Current?.MoveDown());
            ret.KeyboardMap.Add(MoveLeftKey, () => MainCharacter.Current?.MoveLeft());
            ret.KeyboardMap.Add(MoveRightKey, () => MainCharacter.Current?.MoveRight());

            ret.KeyboardMap.Add(AimToggleKey, () => MainCharacter.Current?.ToggleFreeAim());
            ret.KeyboardMap.Add(PrimaryWeaponKey, () => (MainCharacter.Current?.Inventory)?.PrimaryWeapon?.TryFire());
            ret.KeyboardMap.Add(ExplosiveWeaponKey, () => (MainCharacter.Current?.Inventory)?.ExplosiveWeapon?.TryFire());
            return ret;
        }
    }
}
