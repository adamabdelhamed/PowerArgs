using PowerArgs.Cli.Physics;
using System;

namespace ConsoleZombies
{
    public class Cieling : Thing
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
                if(Scene != null)
                {
                    Scene.Update(this);
                }
            }
        }
    }

    [ThingBinding(typeof(Cieling))]
    public class CielingRenderer : ThingRenderer
    {
        public CielingRenderer()
        {
            ZIndex = 100000;
            Background = ConsoleColor.Gray;
        }

        public override void OnRender()
        {
            this.IsVisible = (Thing as Cieling).IsVisible;
        }
    }
}
