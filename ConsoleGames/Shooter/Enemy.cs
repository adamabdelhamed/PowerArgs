using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleGames.Shooter
{
    public class Enemy : ShooterCharacter
    {
        public bool IsBeingTargeted { get; private set; }

        public Enemy()
        {
            this.Inventory = new ShooterInventory(this);
            this.Target = MainCharacter.Current;
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
}
