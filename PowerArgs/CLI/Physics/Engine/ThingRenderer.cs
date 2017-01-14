using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ThingRenderer: ConsoleControl
    {
        public Scene Scene
        {
            get
            {
                return (Parent as ScenePanel)?.Scene;
            }
        }

        public Thing Thing { get; set; }

        public ThingRenderer()
        {
            this.CanFocus = false;
        }

        public virtual void OnBind() { }
        public virtual void OnRender() { }
    }
}
