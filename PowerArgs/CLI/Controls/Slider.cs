using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Cli
{
    public class Slider : ConsoleControl
    {
        public RGB BarColor { get => Get<RGB>(); set => Set(value); }
        public RGB HandleColor { get => Get<RGB>(); set => Set(value); }

        public float Min { get => Get<float>(); set => Set(value); }  
        public float Max { get => Get<float>(); set => Set(value); }  
        public float Value { get => Get<float>(); set => Set(value); }
        public float Increment { get; set; } = 1;
        public bool EnableWAndSKeysForUpDown { get; set; }
        public Slider()
        {
            Min = 0;
            Max = 100;
            Width = 10;
            Height = 1;
            ILifetime focusLt = null;
            
            this.Ready.SubscribeOnce(() =>
            {
                this.SubscribeForLifetime(ObservableObject.AnyProperty, () =>
                {
                    if (Min > Max) throw new InvalidOperationException("Max must be >= Min");
                    if (Value > Max) throw new InvalidOperationException("Value must be <= Max");
                    if (Value < Min) throw new InvalidOperationException("Value must be >= Min");

                }, this);

                this.Focused.SubscribeForLifetime(() =>
                {
                    focusLt?.Dispose();
                    focusLt = new Lifetime();
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.RightArrow, null, SlideUp, focusLt);
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.LeftArrow, null, SlideDown, focusLt);
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, SlideUp, focusLt);
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, SlideDown, focusLt);
                    if (EnableWAndSKeysForUpDown)
                    {
                        Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.D, null, SlideUp, focusLt);
                        Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.A, null, SlideDown, focusLt);
                    }
                }, this);

                this.Unfocused.SubscribeForLifetime(() => focusLt?.Dispose() , this);
            });
        }

        private void SlideUp(ConsoleKeyInfo obj)
        {
            var newVal = Math.Min(Max, Value + Increment);
            Value = newVal;
        }

        private void SlideDown(ConsoleKeyInfo obj)
        {
            var newVal = Math.Max(Min, Value - Increment);
            Value = newVal;
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            context.FillRect(new ConsoleCharacter('-', BarColor, Background), 0, 0, Width, Height);

            var delta = Value - Min;
            var range = Max - Min;
            var percentage = delta / range;
            var left = (int)ConsoleMath.Round(percentage * (Width - 1));

            var barColor = HasFocus ? RGB.Cyan : BarColor;
            context.DrawPoint(new ConsoleCharacter(' ', Background, barColor), left, 0);
        }
    }
}
