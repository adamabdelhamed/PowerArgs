using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames.Shooter
{
    public class LooseWeapon : LooseItem
    {
        public Weapon InnerWeapon { get; private set; }

        public LooseWeapon(Weapon weapon)
        {
            this.InnerWeapon = weapon;
        }

        public override bool CanIncorporate(Character target)
        {
            return target.Inventory != null;
        }

        public override void Incorporate(Character target)
        {
            foreach(var item in target.Inventory.Items)
            {
                if(item.GetType() == InnerWeapon.GetType())
                {
                    (item as Weapon).AmmoAmount += InnerWeapon.AmmoAmount;
                    return;
                }
            }

            target.Inventory.Items.Add(InnerWeapon);
        }
    }

    [SpacialElementBinding(typeof(LooseWeapon))]
    public class LooseWeaponRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context)
        {
            var indicator = (Element as LooseWeapon).InnerWeapon.GetType().Name[0];
            context.Pen = new PowerArgs.ConsoleCharacter(indicator, ConsoleColor.Yellow);
            context.DrawPoint(0, 0);
        }
    }
}
