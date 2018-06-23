using PowerArgs.Cli;
using System;

namespace ConsoleGames.Shooter
{
    public class ShooterKeyMap : ObservableObject
    {
        public ConsoleKey PrimaryWeaponKey { get; set; }
        public ConsoleKey ExplosiveWeaponKey { get; set; }

        public ConsoleKey MoveUpKey { get; set; }
        public ConsoleKey MoveDownKey { get; set; }
        public ConsoleKey MoveLeftKey { get; set; }
        public ConsoleKey MoveRightKey { get; set; }



        public ConsoleKey AimToggleKey { get; set; }

        public ConsoleKey TogglePauseKey { get; set; }
        public ConsoleKey MenuKey { get; set; }
        public ConsoleKey InteractKey { get; set; }

        public ShooterKeyMap()
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

        internal KeyMap GenerateKeyMap()
        {
            var ret = new KeyMap();

            ret.KeyboardMap.Add(MoveUpKey, ( )=>  MainCharacter.Current?.MoveUp());
            ret.KeyboardMap.Add(MoveDownKey, () => MainCharacter.Current?.MoveDown());
            ret.KeyboardMap.Add(MoveLeftKey, () => MainCharacter.Current?.MoveLeft());
            ret.KeyboardMap.Add(MoveRightKey, () => MainCharacter.Current?.MoveRight());

            ret.KeyboardMap.Add(AimToggleKey, () => MainCharacter.Current?.ToggleFreeAim());
            ret.KeyboardMap.Add(PrimaryWeaponKey, () => (MainCharacter.Current?.Inventory as ShooterInventory)?.PrimaryWeapon?.TryFire());
            ret.KeyboardMap.Add(ExplosiveWeaponKey, () => (MainCharacter.Current?.Inventory as ShooterInventory)?.ExplosiveWeapon?.TryFire());
            return ret;
        }
    }
}
