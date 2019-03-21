using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PowerArgs;

namespace PowerArgs.Games
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
            var targets = SpaceTime.CurrentSpaceTime.Elements
                .WhereAs<Character>()
                .Where(c => c is Enemy == false)
                .Where(c => c.Height > 0 && c.Width > 0)
                .OrderBy(c => this.CalculateDistanceTo(c))
                .ToList();

            this.Target = targets.FirstOrDefault();
            IsBeingTargeted = MainCharacter.Current != null && MainCharacter.Current.Target == this;
            targets.Where(t => t.Touches(this)).ForEach(t =>
            {
                t.TakeDamage(5);

                if(t.Lifetime.IsExpired)
                {
                    var newEnemy = SpaceTime.CurrentTime.Add(new Enemy() { Symbol = this.Symbol });
                    var myBot = Time.CurrentTime.Functions.WhereAs<Bot>().Where(b => b.Element == this).SingleOrDefault();
                    if (myBot != null)
                    {
                        var bot = new Bot(newEnemy);
                        bot.Strategy = myBot.Strategy == null ? null : (IBotStrategy)Activator.CreateInstance(myBot.Strategy.GetType());
                    }
                    newEnemy.MoveTo(t.Left, t.Top);
                }

            });
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

            if((Element as Character).Symbol.HasValue)
            {
                context.Pen = new ConsoleCharacter((Element as Character).Symbol.Value, context.Pen.ForegroundColor, context.Pen.BackgroundColor);
            }

            context.FillRect(0, 0, Width, Height);
        }
    }

    public class EnemyReviver : ItemReviver
    {
        public bool TryRevive(LevelItem item, List<LevelItem> allItems, out ITimeFunction hydratedElement)
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
            var bot = new Bot(enemy);
            bot.Strategy = new CustomStrategy();
            return true;
        }
    }

    public class CustomStrategy : BestOfStrategy
    {
        public CustomStrategy()
        {
            this.Children.Add(() => new FireAtWill());
            this.Children.Add(() => new MoveTowardsEnemy());
        }
    }
}
