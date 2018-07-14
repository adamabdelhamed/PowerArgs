using PowerArgs;
using System;

namespace PowerArgs.Games
{
    /// <summary>
    /// The default theme
    /// </summary>
    public class DefaultTheme : Theme
    {
        /// <summary>
        /// Creates the theme
        /// </summary>
        public DefaultTheme()
        {
            Add<WallRenderer>((w) => w.Style = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.White));
            Add<DoorRenderer>((w) => w.Style = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.DarkBlue));
            Add<CeilingRenderer>((w) => w.Style = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Gray));
            Add<ProjectileRenderer>((p) => p.Style = new ConsoleCharacter('*', ConsoleColor.DarkRed));
            Add<PortalRenderer>((p) => p.Style = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.Magenta));
            Add<MainCharacterRenderer>((p) => p.Style = new ConsoleCharacter('M',  ConsoleColor.Magenta));
            Add<CursorRenderer>((p) => p.Style = new ConsoleCharacter('X', ConsoleColor.Blue, ConsoleColor.Cyan));

            Add<LooseWeaponRenderer>((p) => p.Foreground = ConsoleColor.Yellow);
            Add<LooseWeaponRenderer>((p) => p.Background = ConsoleColor.DarkYellow);

            Add<FireRenderer>((p) => p.PrimaryBurnColor = ConsoleColor.Yellow);
            Add<FireRenderer>((p) => p.SecondaryBurnColor = ConsoleColor.Red);
            Add<FireRenderer>((p) => p.BurnSymbol1 = '~');
            Add<FireRenderer>((p) => p.BurnSymbol2 = '-');

            Add<ExplosiveRenderer>((p) => p.Style = new ConsoleCharacter(' ', ConsoleColor.Yellow, ConsoleColor.DarkYellow));

            Add<EnemyRenderer>((p) => p.NormalStyle = new ConsoleCharacter('E', ConsoleColor.Red));
            Add<EnemyRenderer>((p) => p.HurtStyle = new ConsoleCharacter('E', ConsoleColor.DarkRed));
            Add<EnemyRenderer>((p) => p.TargetedStyle = new ConsoleCharacter('E', ConsoleColor.DarkRed, ConsoleColor.Red));
        }
    }
}
