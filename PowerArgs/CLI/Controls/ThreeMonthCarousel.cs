using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{

    public class ThreeMonthCarouselOptions : MonthCalendarOptions
    {
        public float AnimationDuration { get; set; } = 350;
    }

    public class ThreeMonthCarousel : ProtectedConsolePanel
    {
        public ThreeMonthCarouselOptions Options { get; private set; }

        private MonthCalendar left;
        private MonthCalendar center;
        private MonthCalendar right;

        private ILifetime seekLt;

        private ConsoleControl leftPlaceHolder;
        private ConsoleControl centerPlaceHolder;
        private ConsoleControl rightPlaceHolder;

        public ThreeMonthCarousel(ThreeMonthCarouselOptions options = null)
        {
            options = options ?? new ThreeMonthCarouselOptions();
            this.Options = options;
            SetupInvisiblePlaceholders();
            this.SynchronizeForLifetime(nameof(Bounds), Refresh, this);
            SetupKeyboardHandling();
        }

        private void SetupKeyboardHandling()
        {
            if (Options.AdvanceMonthBackwardKey == null || Options.AdvanceMonthForwardKey == null) return;
            this.CanFocus = true;

            this.KeyInputReceived.SubscribeForLifetime(key =>
            {
                var back = Options.AdvanceMonthBackwardKey;
                var fw = Options.AdvanceMonthForwardKey;

                var backModifierMatch = back.Modifier == null || key.Modifiers.HasFlag(back.Modifier);
                if (key.Key == back.Key && backModifierMatch) Seek(false, Options.AnimationDuration);

                var fwModifierMatch = fw.Modifier == null || key.Modifiers.HasFlag(fw.Modifier);
                if (key.Key == fw.Key && fwModifierMatch) Seek(true, Options.AnimationDuration);

            }, this);
        }


        private async void Seek(bool forward, float duration) => await SeekAsync(forward, duration);


        public async Task<bool> SeekAsync(bool forward, float duration)
        {
            if (seekLt != null && seekLt.IsExpired == false) return false;
            using (seekLt = this.CreateChildLifetime())
            {
                var thisMonth = new DateTime(Options.Year, Options.Month, 1);
                thisMonth = thisMonth.AddMonths(forward ? 1 : -1);
                this.Options.Month = thisMonth.Month;
                this.Options.Year = thisMonth.Year;
                var lastMonth = thisMonth.AddMonths(-1);
                var nextMonth = thisMonth.AddMonths(1);

                var leftDest = CalculateLeftDestination();
                var centerDest = CalculateCenterDestination();
                var rightDest = CalculateRightDestination();

                var tempMonth = !forward ? lastMonth : nextMonth;
                var temp = ProtectedPanel.Add(new MonthCalendar(new MonthCalendarOptions() { CustomizeContent = Options.CustomizeContent, MinMonth = Options.MinMonth, MaxMonth = Options.MaxMonth, AdvanceMonthBackwardKey = null, AdvanceMonthForwardKey = null, TodayHighlightColor = Options.TodayHighlightColor, Month = tempMonth.Month, Year = tempMonth.Year }));
                temp.Width = 2;
                temp.Height = 1;
                temp.X = !forward ? -temp.Width : Width + temp.Width;
                temp.Y = ConsoleMath.Round((Height - temp.Height) / 2f);
                var tempDest = !forward ? leftDest : rightDest;

                EasingFunction ease = Animator.EaseInOut;
                var tempAnimation = temp.AnimateAsync(new ConsoleControlAnimationOptions()
                {
                    IsCancelled = () => seekLt.IsExpired,
                    Destination = () => tempDest.ToRectF(),
                    Duration = duration,
                    EasingFunction = ease
                });

                if (!forward)
                {
                    var rightAnimationDest = new RectF(Width + 2, Height / 2, 2, 1);
                    var centerAnimationDest = right.Bounds.ToRectF();
                    var leftAnimationDest = center.Bounds.ToRectF();

                    await Task.WhenAll
                    (
                        right.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = ()=> seekLt.IsExpired, EasingFunction = ease, Destination = () => rightAnimationDest, Duration = duration }),
                        center.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = () => seekLt.IsExpired, EasingFunction = ease, Destination = () => centerAnimationDest, Duration = duration }),
                        left.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = () => seekLt.IsExpired, EasingFunction = ease, Destination = () => leftAnimationDest, Duration = duration }),
                        tempAnimation
                    );

                    right.Dispose();
                    right = center;
                    center = left;
                    left = temp;
                }
                else
                {
                    var rightAnimationDest = ((ICollider)center).Bounds;
                    var centerAnimationDest = ((ICollider)left).Bounds;
                    var leftAnimationDest = new RectF(-2, Height / 2, 2, 1);

                    await Task.WhenAll
                    (
                        right.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = () => seekLt.IsExpired, EasingFunction = ease, Destination = () => rightAnimationDest, Duration = duration }),
                        center.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = () => seekLt.IsExpired, EasingFunction = ease, Destination = () => centerAnimationDest, Duration = duration }),
                        left.AnimateAsync(new ConsoleControlAnimationOptions() { IsCancelled = () => seekLt.IsExpired, EasingFunction = ease, Destination = () => leftAnimationDest, Duration = duration }),
                        tempAnimation
                    );

                    left.Dispose();
                    left = center;
                    center = right;
                    right = temp;

                    left.Bounds = leftDest;
                    center.Bounds = centerDest;
                    right.Bounds = rightDest;

                    await Task.Yield();
                    left.Refresh();
                    right.Refresh();
                    center.Refresh();
                }
                return true;
            }
        }


        private void Refresh()
        {
            if (Width == 0 || Height == 0) return;


            seekLt?.Dispose();
            var leftDest = CalculateLeftDestination();
            var centerDest = CalculateCenterDestination();
            var rightDest = CalculateRightDestination();

            if (center == null)
            {
                var thisMonth = new DateTime(Options.Year, Options.Month, 1);
                var lastMonth = thisMonth.AddMonths(-1);
                var nextMonth = thisMonth.AddMonths(1);
                left = ProtectedPanel.Add(new MonthCalendar(new MonthCalendarOptions() { CustomizeContent = Options.CustomizeContent, MinMonth = Options.MinMonth, MaxMonth = Options.MaxMonth, AdvanceMonthBackwardKey = null, AdvanceMonthForwardKey = null, TodayHighlightColor = Options.TodayHighlightColor, Month = lastMonth.Month, Year = lastMonth.Year }));
                center = ProtectedPanel.Add(new MonthCalendar(new MonthCalendarOptions() { CustomizeContent = Options.CustomizeContent, MinMonth = Options.MinMonth, MaxMonth = Options.MaxMonth, AdvanceMonthBackwardKey = null, AdvanceMonthForwardKey = null, TodayHighlightColor = Options.TodayHighlightColor, Month = thisMonth.Month, Year = thisMonth.Year }));
                right = ProtectedPanel.Add(new MonthCalendar(new MonthCalendarOptions() { CustomizeContent = Options.CustomizeContent, MinMonth = Options.MinMonth, MaxMonth = Options.MaxMonth, AdvanceMonthBackwardKey = null, AdvanceMonthForwardKey = null, TodayHighlightColor = Options.TodayHighlightColor, Month = nextMonth.Month, Year = nextMonth.Year }));

            }

            left.Bounds = leftDest;
            center.Bounds = centerDest;
            right.Bounds = rightDest;
        }


        private void SetupInvisiblePlaceholders()
        {
            var placeholderGrid = ProtectedPanel.Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = new List<GridColumnDefinition>()
                {
                    new GridColumnDefinition(){ Width = .01f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .25f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .01f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .46f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .01f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .25f, Type = GridValueType.Percentage },
                    new GridColumnDefinition(){ Width = .01f, Type = GridValueType.Percentage },
                },
                Rows = new List<GridRowDefinition>()
                {
                    // top margin
                    new GridRowDefinition(){ Height = .1f, Type = GridValueType.Percentage },

                    new GridRowDefinition(){ Height = .15f, Type = GridValueType.Percentage },
                    new GridRowDefinition(){ Height = .5f, Type = GridValueType.Percentage },
                    new GridRowDefinition(){ Height = .15f, Type = GridValueType.Percentage },

                    // bottom margin
                    new GridRowDefinition(){ Height = .1f, Type = GridValueType.Percentage },
                }
            })).Fill();

            placeholderGrid.RefreshLayout();
            leftPlaceHolder = placeholderGrid.Add(new ConsolePanel() { Background = RGB.Green }, 1, 2, 1, 1);
            centerPlaceHolder = placeholderGrid.Add(new ConsolePanel() { Background = RGB.Green }, 3, 1, 1, 3);
            rightPlaceHolder = placeholderGrid.Add(new ConsolePanel() { Background = RGB.Green }, 5, 2, 1, 1);
            placeholderGrid.RefreshLayout();
            placeholderGrid.IsVisible = false;
        }

        private Rectangle CalculateLeftDestination() => MapPlaceholderBoundsToControlBounds(leftPlaceHolder);
        private Rectangle CalculateCenterDestination() => MapPlaceholderBoundsToControlBounds(centerPlaceHolder);
        private Rectangle CalculateRightDestination() => MapPlaceholderBoundsToControlBounds(rightPlaceHolder);

        private Rectangle MapPlaceholderBoundsToControlBounds(ConsoleControl placeholder) => new Rectangle(
                placeholder.AbsoluteX - ProtectedPanel.AbsoluteX,
                placeholder.AbsoluteY - ProtectedPanel.AbsoluteY,
                placeholder.Width, 
                placeholder.Height);
    }
}
