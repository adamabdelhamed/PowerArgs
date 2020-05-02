using PowerArgs.Cli.Physics;
using System;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An object that will allow you do configre the dialog as well as close it.
    /// </summary>
    public class DialogHandle
    {
        private Lifetime callerLifetime = new Lifetime();

        /// <summary>
        /// Set this to override the border color of the dialog. By default the dialog will try to find a dark version of your content's background color.
        /// </summary>
        public RGB? BorderColor { get; set; }

        /// <summary>
        /// Closes the dialog. 
        /// </summary>
        public void CloseDialog() => callerLifetime.Dispose();

        internal DialogHandle() { }

        internal ILifetimeManager CallerLifetime => callerLifetime.Manager;

    }
 
    public class AnimatedDialogOptions
    {
        public ConsolePanel Parent { get; set; }
        public bool PushPop { get; set; } = true;
        public float SpeedPercentage { get; set; } = 1;
    }

    /// <summary>
    /// Utility that lets you add animated dialogs to your ConsoleApps.
    /// </summary>
    public class AnimatedDialog
    {
        /// <summary>
        /// Shows a dialog on top of the current ConsoleApp.
        /// </summary>
        /// <param name="contentFactory">A callback where you are given a handle that can be used to configure the dialog. 
        /// It also has a method that lets you close the dialog. This callback should return the dialog content.</param>
        public static async void Show(Func<DialogHandle,Container> contentFactory, AnimatedDialogOptions options = null)
        {
            options = options ?? new AnimatedDialogOptions(); 
            options.Parent = options.Parent ?? ConsoleApp.Current.LayoutRoot;
            using (var dialogLt = new Lifetime())
            {
                if (options.PushPop)
                {
                    ConsoleApp.Current.FocusManager.Push();
                    dialogLt.OnDisposed(ConsoleApp.Current.FocusManager.Pop);
                }
                var handle = new DialogHandle();
                var content = contentFactory(handle);
                content.IsVisible = false;
                var dialogContainer = options.Parent.Add(new BorderPanel(content) {  BorderColor = handle.BorderColor, Background = content.Background, Width = 1, Height = 1 }).CenterBoth();
                await Forward(300 * options.SpeedPercentage, dialogLt, percentage => dialogContainer.Width = Math.Max(1, Geometry.Round((4+content.Width) * percentage)));
                await Forward(200 * options.SpeedPercentage, dialogLt, percentage => dialogContainer.Height = Math.Max(1, Geometry.Round((2+content.Height) * percentage)));
                content.IsVisible = true;
                await handle.CallerLifetime.AwaitEndOfLifetime();
                content.IsVisible = false;
                await Reverse(150 * options.SpeedPercentage, dialogLt, percentage => dialogContainer.Height = Math.Max(1, (int)Math.Floor((2 + content.Height) * percentage)));
                await Task.Delay((int)(200 * options.SpeedPercentage));
                await Reverse(200 * options.SpeedPercentage, dialogLt, percentage => dialogContainer.Width = Math.Max(1, Geometry.Round((4 + content.Width) * percentage)));
                dialogContainer.Dispose();
            }
        }

        private static Task Forward(float duration, Lifetime lt, Action<float> setter) => AnimateCommon(duration, lt, setter, 0, 1);
        private static Task Reverse(float duration, Lifetime lt, Action<float> setter) => AnimateCommon(duration, lt, setter, 1, 0);
        private static Task AnimateCommon(float duration, Lifetime lt, Action<float> setter, float from, float to) => Animator.AnimateAsync(new FloatAnimatorOptions()
        {
            From = from,
            To = to,
            Duration = duration,
            EasingFunction = Animator.EaseInOut,
            IsCancelled = () => lt.IsExpired,
            Setter = percentage => setter(percentage)
        });
    }
}
