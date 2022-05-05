using System.Diagnostics;
namespace PowerArgs.Cli.Physics;
public class PauseManager
{
    public enum PauseState
    {
        Paused,
        Running
    }
    public IDelayProvider DelayProvider { get; init; } 
    public Event<ILifetimeManager> OnPaused { get; private set; } = new Event<ILifetimeManager>();

    private Lifetime? pauseLifetime;
    private PauseState state;
    public PauseState State
    {
        get => state;
        set
        {
            if (value == state) return;
            state = value;

            if(state == PauseState.Paused)
            {
                pauseLifetime = new Lifetime();
                OnPaused.Fire(pauseLifetime);
            }
            else
            {
                pauseLifetime?.Dispose();
                pauseLifetime = null;
            }
        }
    }

    public PauseManager()
    {
        State = PauseState.Running;
        DelayProvider = new PauseDelayProvider(this);
    }


    public Task Delay(double ms) => Delay((float)ms);
    public Task Delay(TimeSpan span) => Delay(span.TotalMilliseconds);
    public async Task Delay(float ms)
    {
        while(state == PauseState.Paused)
        {
            await Task.Yield();
        }
        var sw = Stopwatch.StartNew();
        while(sw.ElapsedMilliseconds < ms)
        {
            if(state == PauseState.Paused && sw.IsRunning)
            {
                sw.Stop();
            }
            else if(state == PauseState.Running && sw.IsRunning == false)
            {
                sw.Start();
            }
            await Task.Yield();
        }
    }
}

public class PauseDelayProvider : IDelayProvider
{
    private PauseManager manager;
    public PauseDelayProvider(PauseManager manager)
    {
        this.manager = manager;
    }

    public Task DelayAsync(double ms)
    {
        return manager.Delay((float)ms);
    }

    public Task DelayAsync(TimeSpan timeout)
    {
        return manager.Delay(timeout.Milliseconds);
    }

    public Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
    {
        if (timeout.HasValue)
        {
            return TaskEx.WhenAny(ev.CreateNextFireLifetime().AsTask(), DelayAsync(timeout.Value));
        }
        else
        {
            return ev.CreateNextFireLifetime().AsTask();
        }
    }

    public Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
    {
        Task conditionTask = ConsoleApp.Current.InvokeAsync(async () =>
        {
            while(condition() == false)
            {
                await Task.Yield();
                if(evalFrequency.HasValue)
                {
                    await Task.Delay(evalFrequency.Value);
                }
            }
        });

        if (timeout.HasValue)
        {
            return TaskEx.WhenAny(conditionTask, DelayAsync(timeout.Value));
        }
        else
        {
            return conditionTask;
        }
    }

    public Task DelayFuzzyAsync(float ms, double maxDeltaPercentage = 0.1)
    {
        return DelayAsync(ms);
    }

    public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
    {
        await DelayAsync(condition, timeout, evalFrequency);
        return condition();
    }
}

