using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ConsoleGames
{
    public class Enemy : Character
    {
        public bool IsBeingTargeted { get; private set; }

        public Enemy()
        {
            this.Target = MainCharacter.Current;
            this.HealthPoints = 10;
        }

        public override void Evaluate()
        {
            if (MainCharacter.Current == null) return;
            IsBeingTargeted = MainCharacter.Current.Target == this;
        }
    }

    [SpacialElementBinding(typeof(Enemy))]
    public class EnemyRenderer : SpacialElementRenderer
    {
        public bool IsHighlighted { get; private set; }

        public EnemyRenderer()
        {
            this.TransparentBackground = true;
            CanFocus = false;
            ZIndex = 10;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            if ((Element as Enemy).IsBeingTargeted)
            {
                context.Pen = new PowerArgs.ConsoleCharacter('E', (Element as Enemy).HealthPoints < 2 ? ConsoleColor.Gray : ConsoleColor.DarkRed, ConsoleColor.Cyan);
            }
            else
            {
                context.Pen = new PowerArgs.ConsoleCharacter('E', (Element as Enemy).HealthPoints < 2 ? ConsoleColor.Gray : ConsoleColor.DarkRed);
            }
            context.FillRect(0, 0, Width, Height);
        }
    }

    public class EnemyReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, out SpacialElement hydratedElement)
        {
            var enemyTag = item.Tags.Where(testc => testc.Equals("enemy")).SingleOrDefault();
            if (enemyTag == null)
            {
                hydratedElement = null;
                return false;
            }

            var enemy = new Enemy();
            enemy.Inventory.Items.Add(new Pistol() { AmmoAmount = 100 });
            hydratedElement = enemy;
            new Bot(enemy, new List<IBotStrategy> { new FireAtWill(), new MoveTowardsEnemy() });
            return true;
        }
    }
}
