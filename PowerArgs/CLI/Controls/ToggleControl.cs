using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that toggles between on and off
    /// </summary>
    public class ToggleControl : ProtectedConsolePanel
    {
        /// <summary>
        /// Gets or sets the current On / Off value
        /// </summary>
        public bool On { get => Get<bool>(); set => Set(value); }
        private Label valueLabel;

        public string OnLabel { get; set; } =  " On  " ;

        public string OffLabel { get; set; } =  " Off " ;

        /// <summary>
        /// Creates a new ToggleControl
        /// </summary>
        public ToggleControl()
        {
            CanFocus = true;
            Width = 10;
            Height = 1;
            valueLabel = ProtectedPanel.Add(new Label());
            SynchronizeForLifetime(nameof(On), ()=>Update(125), this);
            SynchronizeForLifetime(nameof(IsVisible), () => Update(0), this);
            Focused.SubscribeForLifetime(() => Update(0), this);
            Unfocused.SubscribeForLifetime(() => Update(0), this);
            KeyInputReceived.SubscribeForLifetime(k => On = k.Key == ConsoleKey.Enter ? !On : On, this);
            Ready.SubscribeOnce(() => Update(0));
        }

        private Lifetime valueLifetime;
        private async void Update(float duration)
        {
            valueLifetime?.TryDispose();
            valueLifetime = new Lifetime();

            var newLeft = On ? Width - valueLabel.Width : 0;
            RGB newBarBg;
            RGB newLabelBg;
            RGB newFg;


            if(HasFocus && On)
            {
                newLabelBg = RGB.Cyan;
                newBarBg = RGB.White;
                newFg = RGB.Black; 
            }
            else if(HasFocus)
            {
                newLabelBg = RGB.Cyan;
                newBarBg = RGB.DarkGray;
                newFg = RGB.Black;
            }
            else if(On)
            {
                newLabelBg = RGB.Magenta;
                newBarBg = RGB.White;
                newFg = RGB.Black;
            }
            else
            {
                newLabelBg = RGB.Gray;
                newBarBg = RGB.DarkGray;
                newFg = RGB.Black;
            }

            valueLabel.Text = On ? OnLabel.ToConsoleString(newFg, HasFocus ? RGB.Cyan : newLabelBg) : OffLabel.ToConsoleString(newFg, HasFocus ? RGB.Cyan : newLabelBg);

            if (Application == null)
            {
                valueLabel.X = newLeft;
                ProtectedPanel.Background = newBarBg;
                return;
            }

            var animation1 = Animator.AnimateAsync(new RoundedAnimatorOptions()
            {
                IsCancelled = () => valueLifetime.IsExpired,
                From = valueLabel.Left,
                To = newLeft,
                Duration = duration,
                EasingFunction = Animator.EaseOutSoft,
                Setter = left =>
                {
                    valueLabel.X = left;
                }
            });

            var animation2 = RGB.AnimateAsync(new RGBAnimationOptions()
            {
                IsCancelled = ()=> valueLifetime.IsExpired,
                Duration = duration,
                EasingFunction = Animator.Linear,
                Transitions = new List<KeyValuePair<RGB, RGB>>()
                {
                    new KeyValuePair<RGB, RGB>(Background, newBarBg)
                },
                OnColorsChanged = colors =>
                {
                    ProtectedPanel.Background = colors[0];
                }
            });

            await Task.WhenAll(animation1, animation2);
        }
    }
}
