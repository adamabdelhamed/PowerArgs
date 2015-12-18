using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class FileSystemBrowserApp
    {
        public class FileRecord
        {
            [Filterable]
            public string Name { get; set; }
            public string Path { get; set; }
            public string Type { get; set; }
            public string Length { get; set; }

            public FileRecord(string path)
            {
                this.Path = path;
                this.Name = System.IO.Path.GetFileName(path);
                if(File.Exists(path))
                {
                    Type = "File";
                    Length = new FileInfo(path).Length + "";
                }
                else
                {
                    Type = "Directory";
                }
            }



        }

        public class Drive
        {
            public string Letter { get; set; }
        }

        private Page  explorerPage;

        public FileSystemBrowserApp()
        {
            InitExplorerPage();
        }

 

        private void InitExplorerPage()
        {
            explorerPage = new Page();
            explorerPage.Loaded += () =>
            {
                var path = explorerPage.RouteVariables.ContainsKey("*") ?  explorerPage.RouteVariables["*"] : "";

                GridViewModel gridVm;
                if (path == "")
                {
                    gridVm = new GridViewModel(System.Environment.GetLogicalDrives().Select(d => new Drive() { Letter = d } as object).ToList());
                }
                else
                {
                    List<object> items = new List<object>();
                    items.AddRange(Directory.GetDirectories(path).Select(d => new FileRecord(d)));
                    items.AddRange(Directory.GetFiles(path).Select(d => new FileRecord(d)));
                    gridVm = new GridViewModel(items);
                }

                explorerPage.Controls.Clear();

                var grid = new Grid(gridVm);
                grid.Width = explorerPage.Width;
                grid.Height = explorerPage.Height - 2;
                grid.Y = 2;

                var filter = new TextBox() { Y=1};
                filter.Width = explorerPage.Width;
                grid.FilterTextBox = filter;

                
                    explorerPage.Width = explorerPage.Application.LayoutRoot.Width;
                    explorerPage.Height = explorerPage.Application.LayoutRoot.Height;
                    grid.Width = explorerPage.Width;
                    grid.Height = explorerPage.Height - 2;
                    filter.Width = explorerPage.Width;
              

                var pathColumn = gridVm.VisibleColumns.Where(c => c.ColumnDisplayName.ToString() == "Path").SingleOrDefault();
                if (pathColumn != null)
                {
                    gridVm.VisibleColumns.Remove(pathColumn);
                }

          

                gridVm.SelectedItemActivated += () =>
                {
                    if (gridVm.SelectedItem is Drive)
                    {
                        explorerPage.PageStack.Navigate((gridVm.SelectedItem as Drive).Letter);
                    }
                    else
                    {
                        explorerPage.PageStack.Navigate((gridVm.SelectedItem as FileRecord).Path.Replace('\\','/'));
                    }
                };

                explorerPage.Controls.Add(grid);
                explorerPage.Controls.Add(filter);
            };
        }


        public Task Start()
        {
            ConsolePageApp app = new ConsolePageApp(0, 0, ConsoleProvider.Current.BufferWidth, 20);
            app.PageStack.RegisterDefaultRoute("{*}", () => explorerPage);
            app.PageStack.Navigate("");
            return app.Start();
        }
    }
}
