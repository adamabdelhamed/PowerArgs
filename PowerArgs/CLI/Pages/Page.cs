using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
namespace PowerArgs.Cli
{
    [MarkupIgnore("Route")]
    [MarkupIgnore("IsDefaultRoute")]
    [MarkupIgnore("ViewModelType")]
    public class Page : ConsolePanel
    {
        public ReadOnlyDictionary<string,string> RouteVariables { get; internal set; }

        public string Path { get { return Get<string>(); } internal set { Set(value); } }

        public event Action Loaded;
        public event Action Unloaded;

        public BreadcrumbBar BreadcrumbBar {  get; private set; }

        public ProgressOperationsManager ProgressOperationManager { get; private set; }
        private Dialog progressOperationManagerDialog;

        private IDisposable appResizeSubscription;
        
        public PageStack PageStack
        {
            get
            {
                return (Application as ConsolePageApp).PageStack;
            }
        }

        public Page()
        {
            CanFocus = false;
            ProgressOperationManager = ProgressOperationsManager.Default;
            RemovedFromVisualTree.SubscribeForLifetime(Page_Removed, this.LifetimeManager);
            BreadcrumbBar = Add(new BreadcrumbBar());
            SynchronizeForLifetime(nameof(Bounds), () => { BreadcrumbBar.Width = Width; }, this.LifetimeManager);
        }

        private void Page_Removed()
        {
            if (progressOperationManagerDialog != null && Application.LayoutRoot.Controls.Contains(progressOperationManagerDialog))
            {
                HideProgressOperationsDialog();
            }
        }

        public void ShowProgressOperationsDialog()
        {
            if (PageStack.CurrentPage != this)
            {
                throw new InvalidOperationException("Not the current page");
            }

            if (progressOperationManagerDialog != null && Application.LayoutRoot.Controls.Contains(progressOperationManagerDialog))
            {
                return;
            }

            var progressOperationManagerControl = new ProgressOperationManagerControl(this.ProgressOperationManager);
            progressOperationManagerDialog = new Dialog(progressOperationManagerControl) { MaxHeight = 40 };
            progressOperationManagerDialog.AllowEscapeToCancel = true;

            Application.LayoutRoot.Add(progressOperationManagerDialog);

        }

        public void HideProgressOperationsDialog()
        {
            if (progressOperationManagerDialog != null && Application.LayoutRoot.Controls.Contains(progressOperationManagerDialog))
            {
                Application.LayoutRoot.Controls.Remove(progressOperationManagerDialog);
            }
        }

        internal void Load()
        {
            appResizeSubscription = Application.LayoutRoot.SubscribeUnmanaged(nameof(ConsoleControl.Bounds), HandleResize);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, EscapeKeyHandler, this.LifetimeManager);
            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Backspace, null, BackspaceHandler, this.LifetimeManager);
            OnLoad();
            if (Loaded != null) Loaded();
        }

        internal void Unload()
        {
            appResizeSubscription.Dispose();
            OnUnload();
            if (Unloaded != null) Unloaded();
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUnload() { }

        private void EscapeKeyHandler(ConsoleKeyInfo escape)
        {
            var consolePageApp = (Application as ConsolePageApp);
            if (PageStack.GetSegments(PageStack.CurrentPath).Length > 1)
            {
                PageStack.TryUp();
            }
            else if(consolePageApp.AllowEscapeToExit && consolePageApp.PromptBeforeExit)
            {
                Dialog.ShowMessage("Are you sure you want to quit?".ToConsoleString(), (choice) =>
                 {
                     if(choice != null && choice.DisplayText.StringValue == "Yes")
                     {
                         Application.Stop();
                     }

                 }, true, 10, new DialogButton() { DisplayText = "Yes".ToConsoleString() }, new DialogButton() { DisplayText = "No".ToConsoleString() });
            }
            else if(consolePageApp.AllowEscapeToExit)
            {
                Application.Stop();
            }
        }

        private void BackspaceHandler(ConsoleKeyInfo backspace)
        {
            PageStack.TryBack();
        }

        private void HandleResize()
        {
            this.Size = Application.LayoutRoot.Size;
        }
    }
}
