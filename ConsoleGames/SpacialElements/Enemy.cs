using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PowerArgs;

namespace ConsoleGames
{
    public class Enemy : Character
    {
        public bool IsBeingTargeted { get; private set; }

        public Enemy()
        {
            this.HealthPoints = 10;
        }

        public override void Evaluate()
        {
            if (MainCharacter.Current == null) return;
            this.Target = MainCharacter.Current;
            IsBeingTargeted = MainCharacter.Current.Target == this;

            if(MainCharacter.Current.Touches(this))
            {
                MainCharacter.Current.TakeDamage(5);
            }
        }
    }

    [SpacialElementBinding(typeof(Enemy))]
    public class EnemyRenderer : ThemeAwareSpacialElementRenderer
    {
        public bool IsHighlighted { get; private set; }

        public ConsoleCharacter NormalStyle { get; set; } = new ConsoleCharacter('E', ConsoleColor.White);
        public ConsoleCharacter HurtStyle { get; set; } = new ConsoleCharacter('E', ConsoleColor.DarkGray);
        public ConsoleCharacter TargetedStyle { get; set; } = new ConsoleCharacter('E', ConsoleColor.DarkBlue, ConsoleColor.Cyan);

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
                context.Pen = TargetedStyle;
            }
            else
            {
                context.Pen = (Element as Enemy).HealthPoints >= 3 ? NormalStyle : HurtStyle;
            }
            context.FillRect(0, 0, Width, Height);
        }
    }

    public class EnemyReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out SpacialElement hydratedElement)
        {
            var enemyTag = item.Tags.Where(testc => testc.Equals("enemy")).SingleOrDefault();
            if (enemyTag == null)
            {
                hydratedElement = null;
                return false;
            }

            var enemy = new Enemy();
            enemy.Inventory.Items.Add(new Pistol() { AmmoAmount = 100, HealthPoints = 10, });
            hydratedElement = enemy;
            new Bot(enemy, new List<IBotStrategy> { new FireAtWill(), new MoveTowardsEnemy() });
            return true;
        }
    }
}
