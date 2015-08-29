using PowerArgs.Cli;
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
        /// <summary>
        /// context about the parent reader that is only populated when used by a parent reader.
        /// </summary>
        protected RichCommandLineContext parentReaderContext;
        private IConsoleProvider console;
        private ExpireableAsyncRequestManager expireableAsyncRequestManager;
        private ConsoleWiper menuWiper, resultsWiper;
        private RichTextCommandLineReader searchReader;
        private List<ContextAssistSearchResult> latestResults;
        private string latestResultsSearchString;
        private int selectedIndex;

        /// <summary>
        /// Gets the most recent search result that was selected and committed by the user
        /// </summary>
        public ContextAssistSearchResult SelectedValue { get; private set; }

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
            latestResults = new List<ContextAssistSearchResult>();
            menuWiper = new ConsoleWiper();
            resultsWiper = new ConsoleWiper();
        }

        /// <summary>
        /// When implemented in the derived class, gets search results that match the search string.  This ONLY gets called if SupportsAsync returns false.
        /// </summary>
        /// <param name="searchString">the search string entered by the user</param>
        /// <returns>the search results as a list of strings</returns>
        protected abstract List<ContextAssistSearchResult> GetResults(string searchString);

        /// <summary>
        /// When implemented in the derived class, gets search results that match the search string asynchronously.  This ONLY gets called if SupportsAsync returns true.
        /// </summary>
        /// <param name="searchString">the search string entered by the user</param>
        /// <returns>an async task that will return the search results as a list of strings</returns>
        protected abstract Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString);

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
            try
            {
                DoSearchInternal(parentContext, null, true);
                return ContextAssistResult.CreateInsertResult(parentContext, SelectedValue.RichDisplayText);
            }
            catch (OperationCanceledException)
            {
                return ContextAssistResult.Cancel;
            }
        }

        private void DoSearchInternal(RichCommandLineContext parentContext, IConsoleProvider standaloneConsole, bool allowCancel)
        {
            if(parentContext == null && standaloneConsole == null)
            {
                throw new ArgumentException("You must specify either parentContext or standaloneConsole");
            }
            else if(parentContext != null && standaloneConsole != null)
            {
                throw new ArgumentException("You cannot specify both parentContext and standaloneConsole, you must choose one or the other");
            }

            this.parentReaderContext = parentContext;
            this.console = parentContext != null ? parentContext.Console : standaloneConsole;
            this.menuWiper.Console = this.console;
            this.resultsWiper.Console = this.console;
            this.expireableAsyncRequestManager = new ExpireableAsyncRequestManager();
            SelectedValue = null;

            this.menuWiper.SetTopLeftFromConsole();
            
            if (allowCancel)
            {
                this.console.Write(new ConsoleString("Type to search. Use up/down/enter/escape to navigate/select/cancel: ", ConsoleColor.Cyan));
            }
            else
            {
                this.console.Write(new ConsoleString("Type to search. Use up/down/enter to navigate/select: ", ConsoleColor.Cyan));
            }

            this.resultsWiper.SetTopLeftFromConsole();
            this.resultsWiper.Left = 0;
            this.resultsWiper.Top += 2;

            this.searchReader = new RichTextCommandLineReader() { Console = this.console };
            this.searchReader.UnregisterHandler(ConsoleKey.UpArrow);
            this.searchReader.UnregisterHandler(ConsoleKey.DownArrow);
            this.searchReader.UnregisterHandler(ConsoleKey.Escape);
            this.searchReader.UnregisterHandler(ConsoleKey.Enter);

            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => { _MoveSelectedIndex(-1); searchReaderContext.Intercept = true; }, ConsoleKey.UpArrow));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((_searchReaderContext) => { _searchReaderContext.Intercept = true; _MoveSelectedIndex(1); }, ConsoleKey.DownArrow));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => 
            {
                searchReaderContext.Intercept = true;
                if (allowCancel)
                {
                    throw new OperationCanceledException();
                }
            }, ConsoleKey.Escape));
            this.searchReader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) => { _SearchReader_HandleEnterKey(searchReaderContext); }, ConsoleKey.Enter));
            this.searchReader.AfterReadKey += (searchReaderContext) => { _SearchReader_HandleKeyPressed(searchReaderContext); };

            try
            {
                this.DoSearch(string.Empty);
                this.searchReader.ReadLine();
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

        /// <summary>
        /// Performs a standalone search and cleans up the menu at the end.
        /// </summary>
        /// <param name="console">Optionally choose a custom console target</param>
        /// <param name="allowCancel">if true, users can cancel the search by pressing the escape key.  If false, the escape key does nothing.</param>
        /// <returns>A valid search result or null if the search was cancelled.</returns>
        public ContextAssistSearchResult Search(IConsoleProvider console = null, bool allowCancel = true)
        {
            console = console ?? ConsoleProvider.Current;
            using (var snapshot = console.TakeSnapshot())
            {
                try
                {
                    DoSearchInternal(null, console, allowCancel);
                    return SelectedValue;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                finally
                {
                    ClearMenu(null);
                }
            }
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
                SelectedValue = latestResults[selectedIndex];
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
                    List<ContextAssistSearchResult> results;

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
                            latestResultsSearchString = searchString;
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
            using (var snapshot = this.console.TakeSnapshot())
            {
                resultsWiper.Wipe();
                resultsWiper.SetBottomToTop();
                menuWiper.Bottom = resultsWiper.Bottom;

                this.console.CursorTop = resultsWiper.Top;
                this.console.CursorLeft = 0;


                for (int i = 0; i < latestResults.Count; i++)
                {
                    ConsoleString searchResult = latestResults[i].RichDisplayText;

                    if (i == selectedIndex)
                    {
                        searchResult = searchResult.HighlightSubstring(0, searchResult.Length, ConsoleColor.Yellow, null);
                    }

                    if(searchResult.Length > this.console.BufferWidth - 1)
                    {
                        searchResult = searchResult.Substring(0, this.console.BufferWidth - 4) + "...";
                    }

                    if (latestResultsSearchString.Length > 0)
                    {
                        searchResult = searchResult.Highlight(latestResultsSearchString, ConsoleColor.Black, ConsoleColor.Yellow, StringComparison.InvariantCultureIgnoreCase);
                    }

                    this.console.WriteLine(searchResult);
                    resultsWiper.IncrementBottom();
                    menuWiper.IncrementBottom();
                }
            }
        }
    }
}
