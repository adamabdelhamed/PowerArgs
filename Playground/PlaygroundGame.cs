using ConsoleGames;
using ConsoleGames.Shooter;

namespace Playground
{
    public class PlaygroundGame : ShooterGameApp
    {
        protected override void OnSceneInitialize()
        {
            base.OnSceneInitialize();
            this.MainCharacter.MoveTo(3, 3);

            var enemy = new Enemy();
            enemy.MoveTo(8, 8);
            this.Scene.Add(enemy);

            var weapon = new LooseWeapon(new RPGLauncher() { AmmoAmount = 100000 });
            weapon.MoveTo(20, 15);
            this.Scene.Add(weapon);


            var weapon2 = new LooseWeapon(new Pistol() { AmmoAmount = 100000 });
            weapon2.MoveTo(15, 15);
            this.Scene.Add(weapon2);

            for (var y = 2; y < 17; y++)
            {
                var wall = new Wall();
                wall.MoveTo(60, y);
                this.Scene.Add(wall);
            }
        }
    }
}
