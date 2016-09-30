using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ThingRenderer: ConsoleControl
    {
        public RenderLoop RenderLoop
        {
            get
            {
                return (Parent as RealmPanel)?.RenderLoop;
            }
        }

        public Thing Thing { get; set; }

        public ThingRenderer()
        {
            Background = ConsoleColor.DarkGreen;
            this.CanFocus = false;
        }

        public virtual void OnBind() { }
    }
}
