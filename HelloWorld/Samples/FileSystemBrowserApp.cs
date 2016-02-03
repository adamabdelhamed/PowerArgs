using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private Page CreateExplorerPage()
        {
            var explorerPage = new Page();
 
            explorerPage.Loaded += () =>
            {
                var path = explorerPage.RouteVariables.ContainsKey("*") ? explorerPage.RouteVariables["*"] : "";

                if(path.EndsWith(":"))
                {
                    path += "/";
                }

                List<object> items;
                if (path == "")
                {
                    items = System.Environment.GetLogicalDrives().Select(d => new Drive() { Letter = d } as object).ToList();
                }
                else
                {
                    items = new List<object>();
                    try
                    {
                        items.AddRange(Directory.GetDirectories(path).Select(d => new FileRecord(d)));
                        items.AddRange(Directory.GetFiles(path).Select(d => new FileRecord(d)));
                    }
                    catch (UnauthorizedAccessException) { }
                }

                var grid = explorerPage.Add(new Grid(items));
                var filter = explorerPage.Add(new TextBox() { Y = 1 });

                grid.Width = explorerPage.Width;
                grid.Height = explorerPage.Height - 2;
                grid.Y = 2;


                filter.Width = explorerPage.Width;
                grid.FilterTextBox = filter;


                explorerPage.Width = explorerPage.Application.LayoutRoot.Width;
                explorerPage.Height = explorerPage.Application.LayoutRoot.Height;
                grid.Width = explorerPage.Width;
                grid.Height = explorerPage.Height - 2;
                filter.Width = explorerPage.Width;


                var pathColumn = grid.VisibleColumns.Where(c => c.ColumnDisplayName.ToString() == "Path").SingleOrDefault();
                if (pathColumn != null)
                {
                    grid.VisibleColumns.Remove(pathColumn);
                }



                grid.SelectedItemActivated += () =>
                {
                    if (grid.SelectedItem is Drive)
                    {
                        explorerPage.PageStack.Navigate((grid.SelectedItem as Drive).Letter.Replace('\\', '/'));
                    }
                    else if(Directory.Exists((grid.SelectedItem as FileRecord).Path.Replace('\\', '/')))
                    {
                        explorerPage.PageStack.Navigate((grid.SelectedItem as FileRecord).Path.Replace('\\', '/'));
                    }
                    else
                    {
                        Process.Start((grid.SelectedItem as FileRecord).Path.Replace('\\', '/'));
                    }
                };

                grid.KeyInputReceived += (keyInfo) =>
                {
                    if(keyInfo.Key == ConsoleKey.Delete && grid.SelectedItem != null)
                    {
                        if(grid.SelectedItem is FileRecord)
                        {
                            var deletePath = (grid.SelectedItem as FileRecord).Path;
                            if(File.Exists(deletePath))
                            {
                                Dialog.ShowMessage("Are you sure you want to delete the file ".ToConsoleString() + Path.GetFileName(deletePath).ToConsoleString(ConsoleColor.Yellow) + "?", (response) =>
                                {
                                    if(response != null && response.DisplayText == "Yes")
                                    {
                                        File.Delete(deletePath);
                                        explorerPage.PageStack.Refresh();
                                    }
                                }, true, 12, new DialogButton() { DisplayText = "Yes" }, new DialogButton() { DisplayText = "No" });
                            }
                        }
                    }
                };

                grid.TryFocus();
            };
            return explorerPage;
        }
        


        public Task Start()
        {
            ConsolePageApp app = new ConsolePageApp(0, 0, ConsoleProvider.Current.BufferWidth, 25);
            app.PageStack.RegisterDefaultRoute("{*}", CreateExplorerPage);
            app.PageStack.Navigate("");
            return app.Start();
        }
    }
}
