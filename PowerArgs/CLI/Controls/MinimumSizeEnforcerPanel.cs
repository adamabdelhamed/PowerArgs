using System;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class MinimumSizeEnforcerPanelOptions
    {
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }
        public Action OnMinimumSizeMet { get; set; }
        public Action OnMinimumSizeNotMet { get; set; }
    }

    public class MinimumSizeEnforcerPanel : ConsolePanel
    {
        MinimumSizeEnforcerPanelOptions options;
        private Label messageLabel;
        private Lifetime tooSmallLifetime;
        public MinimumSizeEnforcerPanel(MinimumSizeEnforcerPanelOptions options)
        {
            this.options = options;
            IsVisible = false;
            messageLabel = this.Add(new Label() { Text = "Make that screen bigger yo!".ToYellow() }).CenterBoth();
            this.SubscribeForLifetime(nameof(Bounds), CheckSize, this);
        }

        private void CheckSize()
        {
            if(Width < options.MinWidth || Height < options.MinHeight)
            {
                if (tooSmallLifetime == null)
                {
                    tooSmallLifetime = new Lifetime();
                    IsVisible = true;
                    Application.FocusManager.Push();
                    options.OnMinimumSizeNotMet();
                    OnTooSmall();
                }
            }
            else
            {
                IsVisible = false;
                if (tooSmallLifetime != null)
                {
                    tooSmallLifetime.Dispose();
                    tooSmallLifetime = null;
                    Application.FocusManager.Pop();
                    options.OnMinimumSizeMet();
                }
                else
                {
                    options.OnMinimumSizeMet();
                }
            }
        }

        private async Task OnTooSmall()
        {
            while(tooSmallLifetime != null && tooSmallLifetime.IsExpired == false)
            {
                ConsoleString msg = ConsoleString.Empty;
                if (Width >= 66)
                {
                    var widthNeeded = options.MinWidth - Width;
                    var heightNeeded = options.MinHeight - Height;
                    if (widthNeeded > 0 && heightNeeded > 0)
                    {
                        var colStr = widthNeeded == 1 ? "column" : "columns";
                        var rowStr = heightNeeded == 1 ? "row" : "rows";
                        msg = $"Please make the screen {widthNeeded} {colStr} wider and {heightNeeded} {rowStr} taller".ToYellow();
                    }
                    else if (widthNeeded > 0)
                    {
                        var colStr = widthNeeded == 1 ? "column" : "columns";
                        msg = $"Please make the screen {widthNeeded} {colStr} wider".ToYellow();
                    }
                    else if (heightNeeded > 0)
                    {
                        var rowStr = heightNeeded == 1 ? "row" : "rows";
                        msg = $"Please make the screen {heightNeeded} {rowStr} taller".ToYellow();
                    }
                    else
                    {
                        msg = "Error evaluating minimun screen size".ToRed();
                    }
                }
                else if(Width >= 9)
                {
                    msg = "Too small".ToYellow();
                }
                else
                {
                    msg = "<->".ToYellow();
                }

                messageLabel.Text = msg;
                await Task.Yield();
            }
        }
    }
}
