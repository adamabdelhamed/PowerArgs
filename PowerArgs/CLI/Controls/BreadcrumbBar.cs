using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    internal class BreadcrumbElement : Label
    {
        public BreadcrumbElement(Action activationHandler)
        {
            this.CanFocus = true;
            this.KeyInputReceived += (key) => { if(key.Key == ConsoleKey.Enter)  activationHandler();  };
        }
 
    }
    public class BreadcrumbBar : ConsolePanel
    {
        public PageStack PageStack { get; private set; }
 
        public BreadcrumbBar(PageStack stack)
        {
            this.PageStack = stack;
            this.Height = 1;
            this.CanFocus = false;
            Compose();
        }

        internal void Compose()
        {
            bool hadFocus = this.Controls.Where(c => c.HasFocus).Count() > 0;
            this.Controls.Clear();
 
            string builtUpPath = "";
            foreach(var s in PageStack.GetSegments(PageStack.CurrentPath))
            {
                string myPath;

                if(builtUpPath == "")
                {
                    builtUpPath = s;
                    myPath = s;
                }
                else
                {
                    this.Controls.Add(new Label() { Text = "->".ToConsoleString(Theme.DefaultTheme.H1Color) });
                    builtUpPath += "/" + s;
                    myPath = builtUpPath;
                }

                var crumb = Add(new BreadcrumbElement(() => { PageStack.TryNavigate(myPath); }) { Text = s.ToConsoleString() });

                if(hadFocus && builtUpPath.Contains("/") == false)
                {
                    var worked = crumb.TryFocus();
                }
    
            }

            Layout.StackHorizontally(1, this.Controls);
        }

        
    }
}
