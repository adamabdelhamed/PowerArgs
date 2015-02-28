using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// A context assist provider that lets a user search for an option.  The class is abstract.  Implementors just need to define the search implementation
    /// and this class will do the rest.
    /// </summary>
    public abstract class ContextAssistSearch : IContextAssistProvider
    {
        private RichCommandLineContext parentReaderContext;
        private ExpireableAsyncRequestManager expireableAsyncRequestManager;
        private ConsoleWiper menuWiper, resultsWiper;
        private RichTextCommandLineReader searchReader;
        private List<string> latestResults;
        private int selectedIndex;
        private ConsoleString selection;

        /// <summary>
        /// True indicates that the derived class has implemented GetResultsAsync(), which will be called by the base class.  If false then
        /// the base class will call GetResults() and will never call GetResultsAsync() so you can choose not to implement it.
        /// </summary>
        public abstract bool SupportsAsync { get;  }

        /// <summary>
        /// Initializes the search assist class
        /// </summary>
        public ContextAssistSearch()
        {
            latestResults = new List<string>();
            menuWiper = new ConsoleWiper();
            resultsWiper = new ConsoleWiper();
        }

        /// <summary>
        /// When implemented in the derived class, gets search results that match the search string.  This ONLY gets called if SupportsAsync returns false.
        /// </summary>
        /// <param name="searchString">the search string entered by the user</param>
        /// <returns>the search results as a list of strings</returns>
        protected abstract List<string> GetResults(string searchString);

        /// <summary>
        /// When implemented in the derived class, gets search results that match the search string asynchronously.  This ONLY gets called if SupportsAsync returns true.
        /// </summary>
        /// <param name="searchString">the search string entered by the user</param>
        /// <returns>an async task that will return the search results as a list of strings</returns>
        protected abstract Task<List<string>> GetResultsAsync(string searchString);

        /// <summary>
        /// Always returns true.  When overrided in a derived class the derived class can provide custom logic to determine whether or not this assist provider
        /// can assist.
        /// </summary>
        /// <param name="parentContext">context about the parent reader that we may be assisting </param>
        /// <returns>Always returns true.  When overrided in a derived class the derived class can provide custom logic to determine whether or not this assist provider
        /// can assist.</returns>
        public virtual bool CanAssist(RichCommandLineContext parentContext) { return true; }

        /// <summary>
        /// This is not implemented because this assist provider always takes over the console during the draw menu.
        /// </summary>
        /// <param name="parentReaderContext">not implemented</param>
        /// <param name="keyPress">not implemented</param>
        /// <returns>not implemented</returns>
        public virtual ContextAssistResult OnKeyboardInput(RichCommandLineContext parentReaderContext, ConsoleKeyInfo keyPress) { throw new NotImplementedException(); }

        /// <summary>
        /// Writes the prompt message and takes over the console until the user makes a selection or cancels via the escape key.  This method
        /// never returns a NoOp result.
        /// </summary>
        /// <param name="parentContext">context about the parent reader that we are assisting </param>
        /// <returns>A selection or cancellation result, never a NoOp</returns>
        public virtual ContextAssistResult DrawMenu(RichCommandLineContext parentContext)
        {
            this.parentReaderContext = parentContext;
            this.menuWiper.Console = parentReaderContext.Console;
            this.resultsWiper.Console = parentReaderContext.Console;
            this.expireableAsyncRequestManager = new ExpireableAsyncRequestManager();
            this.selection = null;

            this.menuWiper.SetTopLeftFromConsole();
            this.parentReaderContext.Console.Write(new ConsoleString("Type to search/filter.  Use up/down/enter to navigate and select: ", ConsoleColor.Cyan));
            this.resultsWiper.SetTopLeftFromConsole();
            this.resultsWiper.Left = 0;
            this.resultsWiper.Top += 2;

            this.searchReader = new RichTextCommandLineReader();
            this.searchReader.UnregisterHandler(ConsoleKey.UpArrow);
            this.searchReader.UnregisterHandler(ConsoleKey.DownArrow);
            this.searchReader.UnregisterHandler(ConsoleKey.Escape);
            this.searchReader.UnregisterHandler(ConsoleKey.Enter);

            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => { _MoveSelectedIndex(-1); searchReaderContext.Intercept = true; }, ConsoleKey.UpArrow));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((_searchReaderContext) => { _searchReaderContext.Intercept = true; _MoveSelectedIndex(1); }, ConsoleKey.DownArrow));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => { searchReaderContext.Intercept = true; throw new OperationCanceledException(); }, ConsoleKey.Escape));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => { _SearchReader_HandleEnterKey(searchReaderContext); }, ConsoleKey.Enter));
            this.searchReader.AfterReadKey += (searchReaderContext) => { _SearchReader_HandleKeyPressed(searchReaderContext); };

            try
            {
                this.DoSearch(string.Empty);
                this.searchReader.ReadLine();
                return ContextAssistResult.CreateInsertResult(parentReaderContext, selection);
            }
            catch (OperationCanceledException)
            {
                return ContextAssistResult.Cancel;
            }
            finally
            {
                // This next lione makes sure that we ignore any in flight search calls that come back after we return control of the main
                // thread.  If we didn't do this then the results would get written to the screen even though the search assist code is no
                // longer running.
                this.expireableAsyncRequestManager.ExpireAll();
            }
        }

        /// <summary>
        /// Clears the menu from the console
        /// </summary>
        /// <param name="notUsed"></param>
        public virtual void ClearMenu(RichCommandLineContext notUsed)
        {
            menuWiper.Wipe();
        }

        private void _SearchReader_HandleKeyPressed(RichCommandLineContext searchReaderContext)
        {
            if(searchReaderContext.KeyPressed.Key == ConsoleKey.UpArrow || searchReaderContext.KeyPressed.Key == ConsoleKey.DownArrow)
            {
                return;
            }
            else
            {
                DoSearch(searchReaderContext.Buffer.ToNormalString());
            }
        }

        private void _SearchReader_HandleEnterKey(RichCommandLineContext searchReaderContext)
        {
            searchReaderContext.Intercept = true;
            if (latestResults.Count > 0)
            {
                selection = new ConsoleString(latestResults[selectedIndex]);
                searchReaderContext.IsFinished = true;
            }
        }

        private void _MoveSelectedIndex(int amount)
        {
            selectedIndex += amount;
            if (selectedIndex < 0)
            {
                selectedIndex = latestResults.Count - 1;
            }
            else if (selectedIndex >= latestResults.Count)
            {
                selectedIndex = 0;
            }
            RedrawSearchResults();
        }

        private void DoSearch(string searchString)
        {
            // as soon as this next line runs, any in flight searches become invalid
            var myRequestId = expireableAsyncRequestManager.BeginRequest();
            var backgroundSearchTask = new Task(() =>
            {
                try
                {
                    List<string> results;

                    try
                    {
                        if (SupportsAsync)
                        {
                            var userAsyncTask = GetResultsAsync(searchString);
                            userAsyncTask.Wait();
                            results = userAsyncTask.Result;
                        }
                        else
                        {
                            results = GetResults(searchString);
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerLogger.LogLine("Exception fetching results on search provider: " + GetType().FullName + "\n\n" + ex);
                        return;
                    }

                    lock (searchReader.SyncLock)
                    {
                        expireableAsyncRequestManager.EndRequest(() =>
                        {
                            // this block of code only runs if these results are from the most recent request and
                            // the original called of the search assist still cares about results.

                            selectedIndex = 0;
                            latestResults.Clear();
                            latestResults.AddRange(results);
                            RedrawSearchResults();
                        }, myRequestId);
                    }
                }catch(Exception ex)
                {
                    PowerLogger.LogLine("Background exception is search provider: " + GetType().FullName + "\n\n" + ex);
                }

            });
            backgroundSearchTask.Start();
        }

        private void RedrawSearchResults()
        {
            var leftNow = parentReaderContext.Console.CursorLeft;
            var topNow = parentReaderContext.Console.CursorTop;

            resultsWiper.Wipe();
            resultsWiper.SetBottomToTop();
            menuWiper.Bottom = resultsWiper.Bottom;

            parentReaderContext.Console.CursorTop = resultsWiper.Top;
            parentReaderContext.Console.CursorLeft = 0;

            for (int i = 0; i < latestResults.Count; i++)
            {
                ConsoleColor? fg = null;
                ConsoleColor? bg = null;
                if (i == selectedIndex)
                {
                    fg = ConsoleColor.Yellow;
                }
                parentReaderContext.Console.WriteLine(new ConsoleString(latestResults[i].ToString(), fg, bg));
                resultsWiper.IncrementBottom();
                menuWiper.IncrementBottom();
            }

            parentReaderContext.Console.CursorLeft = leftNow;
            parentReaderContext.Console.CursorTop = topNow;
        }
    }
}
