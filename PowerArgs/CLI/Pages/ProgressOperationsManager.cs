using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ProgressOperationsManager : ObservableObject
    {
        public event Action ProgressOperationsChanged;

        public ObservableCollection<ProgressOperation> Operations { get; private set; }

        public static readonly ProgressOperationsManager Default = new ProgressOperationsManager();

        Subscription addSub;
        Subscription removeSub;

        public ProgressOperationsManager()
        {
            Operations = new ObservableCollection<ProgressOperation>();
            addSub = Operations.Added.SubscribeUnmanaged(Operations_Added);
            removeSub = Operations.Removed.SubscribeUnmanaged(Operations_Removed);
        }

        

        private void Operations_Added(ProgressOperation trackedOperation)
        {
            trackedOperation.SynchronizeForLifetime(AnyProperty, FireProgressOperationsChanged, Operations.GetMembershipLifetime(trackedOperation));
            FirePropertyChanged(nameof(Operations)); // todo - remove this, but find the subscriber first
        }

        private void Operations_Removed(ProgressOperation trackedOperation)
        {
            FireProgressOperationsChanged();
            FirePropertyChanged(nameof(Operations)); // todo - remove this, but find the subscriber first
        }

        private void FireProgressOperationsChanged()
        {
            if (ProgressOperationsChanged != null)
            {
                ProgressOperationsChanged();
            }
        }
    }

    public enum OperationState
    {
        NotSet,
        Scheduled,
        Queued,
        InProgress,
        Completed,
        CompletedWithWarnings,
        Failed,
    }

    public class ProgressOperation : ObservableObject
    {
        public OperationState State { get { return Get<OperationState>();} set { Set(value); } }
        public ConsoleString Message { get { return Get<ConsoleString>(); } set { Set(value); } }
        public ConsoleString Details { get { return Get<ConsoleString>(); } set { Set(value); } }
        public double Progress { get { return Get<double>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { Set(value); } }
        public DateTime? EndTime { get { return Get<DateTime>(); } set { Set(value); } }
        public DateTime LastUpdatedTime { get { return Get<DateTime>(); } set { Set(value); } }

        public ObservableCollection<ProgressOperationAction> Actions { get; private set; }

        Subscription selfSub;

        public ProgressOperation()
        {
            StartTime = DateTime.Now;
            LastUpdatedTime = StartTime;
            State = OperationState.NotSet;
            Message = "".ToConsoleString();
            Progress = -1;
            Actions = new ObservableCollection<ProgressOperationAction>();
            selfSub = SubscribeUnmanaged(nameof(State), StateChanged);
        }

        private void StateChanged()
        {
            LastUpdatedTime = DateTime.Now;

            // if the state is terminal and we've not yet recorded an end time then record it
            if(EndTime.HasValue == false && (State == OperationState.Completed || State == OperationState.CompletedWithWarnings || State == OperationState.Failed))
            {
                EndTime = DateTime.Now;
            }
        }
    }

    public class ProgressOperationAction
    {
        public string DisplayName { get; set; }

        public Action Action { get; set; }
    }
}
