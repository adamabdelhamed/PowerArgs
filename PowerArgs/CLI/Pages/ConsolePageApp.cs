using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ConsolePageApp : ConsoleApp
    {
        public PageStack PageStack { get; private set; }


        public bool AllowEscapeToExit { get; set; }
        public bool PromptBeforeExit { get; set; }

        public ConsolePageApp(int x, int y, int w, int h) : base(x, y, w, h)
        {
            InitCommon();
        }

        public ConsolePageApp() : base()
        {
            InitCommon();
        }

        private void InitCommon()
        {
            this.PageStack = new PageStack();
            this.PageStack.PropertyChanged += PageStack_PropertyChanged;
            this.SetFocusOnStart = false;
            AllowEscapeToExit = true;
            PromptBeforeExit = true;
        }

        private void PageStack_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(PageStack.CurrentPage))
            {
                var currentPage = LayoutRoot.Controls.Where(c => c is Page).FirstOrDefault() as Page;

                if(currentPage != null)
                {
                    currentPage.Unload();
                }

                LayoutRoot.Controls.Clear();
                LayoutRoot.Controls.Add(PageStack.CurrentPage);
                PageStack.CurrentPage.Width = LayoutRoot.Width;
                PageStack.CurrentPage.Height = LayoutRoot.Height;
                PageStack.CurrentPage.Load();

                if (PageStack.CurrentPage.ShowBreadcrumbBar)
                {
                    PageStack.CurrentPage.BreadcrumbBar.Compose();
                }

         //       FocusManager.TryRestoreFocus();
               
                Paint();
            }
        }
    }
}
