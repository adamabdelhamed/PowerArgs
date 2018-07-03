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
            this.KeyInputReceived.SubscribeForLifetime((key) => 
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    activationHandler();
                }
            }, this);
        }
 
    }
    public class BreadcrumbBar : ConsolePanel
    {
        public BreadcrumbBar()
        {
            this.Height = 1;
            this.CanFocus = false;
            AddedToVisualTree.SubscribeForLifetime(Compose, this);
        }

        internal void Compose()
        {
            var pageStack = (Application as ConsolePageApp).PageStack;
            bool hadFocus = this.Controls.Where(c => c.HasFocus).Count() > 0;
            this.Controls.Clear();
 
            string builtUpPath = "";
            foreach(var s in PageStack.GetSegments(pageStack.CurrentPath))
            {
                string myPath;

                if(builtUpPath == "")
                {
                    builtUpPath = s;
                    myPath = s;
                }
                else
                {
                    var label = Add(new Label() { Mode = LabelRenderMode.SingleLineAutoSize, Text = "->".ToConsoleString(Theme.DefaultTheme.H1Color) });
                    builtUpPath += "/" + s;
                    myPath = builtUpPath;
                }

                var crumb = Add(new BreadcrumbElement(() => { pageStack.TryNavigate(myPath); }) { Text = s.ToConsoleString() });
                crumb.Width = 10;
                if(hadFocus && builtUpPath.Contains("/") == false)
                {
                    var worked = crumb.TryFocus();
                }
    
            }

            this.Width = Layout.StackHorizontally(1, this.Controls);
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            base.OnPaint(context);
        }

    }
}
