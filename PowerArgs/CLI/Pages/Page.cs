using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
namespace PowerArgs.Cli
{
    public class Page : ConsolePanel
    {
        public ReadOnlyDictionary<string,string> RouteVariables { get; internal set; }

        public string Path { get { return Get<string>(); } internal set { Set(value); } }

        public event Action Loaded;
        public event Action Unloaded;

        public BreadcrumbBar BreadcrumbBar {  get; private set; }

        public ProgressOperationsManager ProgressOperationManager { get; private set; }
        private Dialog progressOperationManagerDialog;

        private PropertyChangedEventHandler appResizeHandler;

        bool _showBreadcrumbBar;
        public bool ShowBreadcrumbBar
        {
            get
            {
                return _showBreadcrumbBar;
            }
            set
            {
                _showBreadcrumbBar = value;
                if (Application != null)
                {
                    if (value && Controls.Where(c => c == BreadcrumbBar).Count() == 1)
                    {
                        // already added
                    }
                    else if (value)
                    {
                        BreadcrumbBar = BreadcrumbBar ?? new BreadcrumbBar(PageStack);
                        BreadcrumbBar.Width = Width;
                        this.PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == nameof(Bounds))
                            {
                                BreadcrumbBar.Width = Width;
                            }
                        };
                        Controls.Insert(0, BreadcrumbBar);
                    }
                }
            }
        }


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
            ShowBreadcrumbBar = true;
            ProgressOperationManager = ProgressOperationsManager.Default;
            Removed += Page_Removed;
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
            if(PageStack.CurrentPage != this)
            {
                throw new InvalidOperationException("Not the current page");
            }

            if(progressOperationManagerDialog != null && Application.LayoutRoot.Controls.Contains(progressOperationManagerDialog))
            {
                return;
            }

            if (progressOperationManagerDialog == null)
            {
                var progressOperationManagerControl = new ProgressOperationManagerControl(this.ProgressOperationManager);
                progressOperationManagerDialog = new Dialog(progressOperationManagerControl);
                progressOperationManagerDialog.AllowEscapeToCancel = true;
            }

            Application.LayoutRoot.Add(progressOperationManagerDialog);
            
        }

        public void HideProgressOperationsDialog()
        {
            if (progressOperationManagerDialog != null && Controls.Contains(progressOperationManagerDialog))
            {
                Application.LayoutRoot.Controls.Remove(progressOperationManagerDialog);
            }
        }

        internal void Load()
        {
            appResizeHandler = Application.LayoutRoot.Subscribe(nameof(ConsoleControl.Bounds), HandleResize);
            Application.GlobalKeyHandlers.Push(ConsoleKey.Escape, EscapeKeyHandler);
            Application.GlobalKeyHandlers.Push(ConsoleKey.Backspace, BackspaceHandler);
            if(ShowBreadcrumbBar)
            {
                ShowBreadcrumbBar = true;
            }
            OnLoad();
            if (Loaded != null) Loaded();
        }

        internal void Unload()
        {
            Application.LayoutRoot.Unsubscribe(appResizeHandler);
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Escape);
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Backspace);
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
                     if(choice != null && choice.DisplayText == "Yes")
                     {
                         Application.MessagePump.Stop();
                     }

                 }, true, new DialogButton() { DisplayText = "Yes" }, new DialogButton() { DisplayText = "No" });
            }
            else if(consolePageApp.AllowEscapeToExit)
            {
                Application.MessagePump.Stop();
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
