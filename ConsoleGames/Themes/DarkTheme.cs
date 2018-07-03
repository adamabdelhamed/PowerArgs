using PowerArgs;
using System;

namespace ConsoleGames
{
    /// <summary>
    /// A dark theme
    /// </summary>
    public class DarkTheme : Theme
    {
        /// <summary>
        /// Creates the theme
        /// </summary>
        public DarkTheme()
        {
            Add<WallRenderer>((w) => w.Style = new ConsoleCharacter('#', ConsoleColor.DarkGray));
            Add<DoorRenderer>((w) => w.Style = new ConsoleCharacter('|', ConsoleColor.Gray));
            Add<CeilingRenderer>((w) => w.Style = new ConsoleCharacter('-', ConsoleColor.Gray));
            Add<ProjectileRenderer>((p) => p.Style = new ConsoleCharacter('*', ConsoleColor.Red));
            Add<PortalRenderer>((p) => p.Style = new ConsoleCharacter(' ', backgroundColor: ConsoleColor.White));
            Add<MainCharacterRenderer>((p) => p.Style = new ConsoleCharacter('M',  ConsoleColor.DarkGray));
            Add<CursorRenderer>((p) => p.Style = new ConsoleCharacter('X', ConsoleColor.DarkBlue, ConsoleColor.DarkGray));

            Add<LooseWeaponRenderer>((p) => p.Foreground = ConsoleColor.Gray);
            Add<LooseWeaponRenderer>((p) => p.Background = ConsoleColor.DarkGray);

            Add<FireRenderer>((p) => p.PrimaryBurnColor = ConsoleColor.Red);
            Add<FireRenderer>((p) => p.SecondaryBurnColor = ConsoleColor.Yellow);
            Add<FireRenderer>((p) => p.BurnSymbol1 = '~');
            Add<FireRenderer>((p) => p.BurnSymbol2 = '-');

            Add<ExplosiveRenderer>((p) => p.Style = new ConsoleCharacter('E', ConsoleColor.Gray, ConsoleColor.DarkGray));

            Add<EnemyRenderer>((p) => p.NormalStyle = new ConsoleCharacter('E', ConsoleColor.White));
            Add<EnemyRenderer>((p) => p.HurtStyle = new ConsoleCharacter('E', ConsoleColor.DarkRed));
            Add<EnemyRenderer>((p) => p.TargetedStyle = new ConsoleCharacter('E', ConsoleColor.Black, ConsoleColor.White));
        }
    }
}
