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

        private ObservableCollection<ProgressOperation> operations;

        public static readonly ProgressOperationsManager Default = new ProgressOperationsManager();

        public ProgressOperationsManager()
        {
            operations = new ObservableCollection<ProgressOperation>();
            operations.Added += Operations_Added;
            operations.Removed += Operations_Removed;
        }

        public void Add(ProgressOperation operation)
        {
            operations.Add(operation);
        }

        public bool Remove(ProgressOperation operation)
        {
            return operations.Remove(operation);
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
        public Action ActivateAction { get; set; }

        public ProgressOperation()
        {
            State = OperationState.NotSet;
            Message = "".ToConsoleString();
            Progress = -1;
        }
    }

    internal class ProgressOperationCommand
    {
        public string DisplayName { get; set; }
        
    }
}
