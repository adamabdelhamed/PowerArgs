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

        BreadcrumbBar bar;
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
                    if (value && Controls.Where(c => c == bar).Count() == 1)
                    {
                        // already added
                    }
                    else if (value)
                    {
                        bar = bar ?? new BreadcrumbBar(PageStack);
                        bar.Width = Width;
                        this.PropertyChanged += (sender, e) =>
                        {
                            if (e.PropertyName == nameof(Bounds))
                            {
                                bar.Width = Width;
                            }
                        };
                        Controls.Add(bar);
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
                Application.MessagePump.Stop();
            }
        }

        private void BackspaceHandler(ConsoleKeyInfo backspace)
        {
            PageStack.TryBack();
        }
    }
}
