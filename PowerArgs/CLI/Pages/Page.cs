using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                        Controls.Add(BreadcrumbBar);
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
        }

        internal void Load()
        {
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
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Escape);
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Backspace);
            OnUnload();
            if (Unloaded != null) Unloaded();
        }

        protected virtual void OnLoad() { }
        protected virtual void OnUnload() { }

        private void EscapeKeyHandler(ConsoleKeyInfo escape)
        {
            if (PageStack.GetSegments(PageStack.CurrentPath).Length > 1)
            {
                PageStack.TryUp();
            }
            else
            {
                Dialog.Show("Are you sure you want to quit?".ToConsoleString(), (choice) =>
                 {
                     if(choice != null && choice.DisplayText == "Yes")
                     {
                         Application.MessagePump.Stop();
                     }

                 }, true, new DialogButton() { DisplayText = "Yes" }, new DialogButton() { DisplayText = "No" });
            }
        }

        private void BackspaceHandler(ConsoleKeyInfo backspace)
        {
            PageStack.TryBack();
        }
    }
}
