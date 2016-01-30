using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ProgressOperationsManager
    {
        public event Action ProgressOperationsChanged;

        public ObservableCollection<ProgressOperation> Operations { get; private set; }

        public static readonly ProgressOperationsManager Default = new ProgressOperationsManager();

        public ProgressOperationsManager()
        {
            Operations = new ObservableCollection<ProgressOperation>();
            Operations.Added += Operations_Added;
            Operations.Removed += Operations_Removed;
        }

        

        private void Operations_Added(ProgressOperation trackedOperation)
        {
            trackedOperation.PropertyChanged += TrackedOperation_PropertyChanged;
            FireProgressOperationChanged();
        }

        private void Operations_Removed(ProgressOperation trackedOperation)
        {
            trackedOperation.PropertyChanged -= TrackedOperation_PropertyChanged;
            FireProgressOperationChanged();
        }

        private void TrackedOperation_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            FireProgressOperationChanged();
        }

        private void FireProgressOperationChanged()
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

    public class ProgressOperation : ViewModelBase
    {
        public OperationState State { get { return Get<OperationState>();} set { Set(value); } }
        public ConsoleString Message { get { return Get<ConsoleString>(); } set { Set(value); } }
        public ConsoleString Details { get { return Get<ConsoleString>(); } set { Set(value); } }
        public double Progress { get { return Get<double>(); } set { Set(value); } }

        public DateTime StartTime { get { return Get<DateTime>(); } set { Set(value); } }
        public DateTime? EndTime { get { return Get<DateTime>(); } set { Set(value); } }
        public DateTime LastUpdatedTime { get { return Get<DateTime>(); } set { Set(value); } }

        public ObservableCollection<ProgressOperationAction> Actions { get; private set; }


        public ProgressOperation()
        {
            StartTime = DateTime.Now;
            LastUpdatedTime = StartTime;
            State = OperationState.NotSet;
            Message = "".ToConsoleString();
            Progress = -1;
            Actions = new ObservableCollection<ProgressOperationAction>();
            this.PropertyChanged += ProgressOperation_PropertyChanged;
        }

        private void ProgressOperation_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName != nameof(LastUpdatedTime))
            {
                LastUpdatedTime = DateTime.Now;
            }
            
            // if the state is terminal and we've not yet recorded an end time then record it
            if(e.PropertyName == nameof(State) && EndTime.HasValue == false && (State == OperationState.Completed || State == OperationState.CompletedWithWarnings || State == OperationState.Failed))
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
