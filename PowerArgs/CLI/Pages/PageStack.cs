using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
namespace PowerArgs.Cli
{
    public class PageStack : INotifyPropertyChanged
    {
        private Stack<KeyValuePair<string,Page>> stack;

        private Dictionary<string, Func<Page>> routes;
        private KeyValuePair<string, Func<Page>>? defaultRoute;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Count
        {
            get
            {
                return stack.Count;
            }
        }

        public ReadOnlyCollection<KeyValuePair<string, Page>> PagesInStack
        {
            get
            {
                return stack.ToList().AsReadOnly();
            }
        }

        public Page CurrentPage
        {
            get
            {
                if (stack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return stack.Peek().Value;
                }
            }
        }

        public string CurrentPath
        {
            get
                {
                if (stack.Count == 0)
                {
                    return null;
                }
                else
                {
                    return stack.Peek().Key;
                }
            }
            set
            {
                Page page;
                if (TryResolveRoute(ref value, out page) == false)
                {
                    throw new KeyNotFoundException("There is no page that matches the path '" + value + "'");
                }

                stack.Clear();
                Push(value, page);
            }
        }

        public PageStack()
        {
            stack = new Stack<KeyValuePair<string, Page>>();
            routes = new Dictionary<string, Func<Page>>();
        }

        public void Navigate(string path)
        {
            if (TryNavigate(path) == false)
            {
                throw new KeyNotFoundException("There is no page that matches the path '" + path + "'");
            }
        }

        public bool TryNavigate(string path)
        {
            Page page;
            if(TryResolveRoute(ref path, out page) == false)
            {
                return false;
            }
            Push(path, page);
            return true;
        }

        private void Push(string route, Page p)
        {
            stack.Push(new KeyValuePair<string, Page>(route, p));
            FirePropertyChanged(nameof(CurrentPage));
            FirePropertyChanged(nameof(CurrentPath));
        }

        public bool TryBack()
        {
            if (stack.Count == 0) return false;
            if (stack.Count == 1 && routes.ContainsKey("*") == false) return false;

            stack.Pop();

            if (stack.Count == 0 && routes.ContainsKey("*"))
            {
                Navigate("");
            }
            else
            {
                FirePropertyChanged(nameof(CurrentPage));
                FirePropertyChanged(nameof(CurrentPath));
            }

            return true;
        }

        public bool TryUp()
        {
            if (CurrentPath == null)
            {
                return false;
            }

            if (CurrentPath.IndexOf('/') < 0)
            {
                if(routes.ContainsKey("*") == false)
                {
                    return false;
                }
                else
                {
                    Navigate("*");
                    return true;
                }
            }
            else
            {
                var newPath = CurrentPath.Substring(0, CurrentPath.LastIndexOf('/'));
                Page p;
                if(TryResolveRoute(ref newPath, out p))
                {
                    Push(newPath, p);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void RegisterRoute(string route, Func<Page> pageFactory)
        {

            if (route.EndsWith("/")) route = route.Substring(0, route.Length - 1);
            if (Regex.IsMatch(route, @"^{?[a-zA-Z0-9_]+}?(\/{?[a-zA-Z0-9_]+}?)*$") == false)
            {
                throw new FormatException("Routes must be made up of alphanumeric characters or underscores separated by '/' characters.  Segments can be surrounded with {} to represent a variable");
            }


            routes.Add(route, pageFactory);
        }

        public void RegisterDefaultRoute(string path, Func<Page> pageFactory)
        {
            if(defaultRoute.HasValue)
            {
                routes.Remove(defaultRoute.Value.Key);
            }
            defaultRoute = new KeyValuePair<string, Func<Page>>(path, pageFactory);
            RegisterRoute(path, pageFactory);
        }

        public static string [] GetSegments(string path)
        {
            var segments = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return segments;
        }


        private bool TryResolveRoute(ref string path, out Page page)
        {
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);

            if(path == "")
            {
                if(defaultRoute.HasValue)
                {
                    path = defaultRoute.Value.Key;
                    page = defaultRoute.Value.Value();
                    page.RouteVariables = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
                    page.Path = defaultRoute.Value.Key;
                    return true;
                }
                else
                {
                    page = null;
                    return false;
                }
            }

            var pathSegments = path.Split('/');
            foreach (var route in routes.Keys)
            {
                var variablesCandidate = new Dictionary<string, string>();
                var routeSegments = route.Split('/');

                if(routeSegments.Length != pathSegments.Length)
                {
                    continue;
                }

                bool candidateIsGood = true;
                for(int i = 0; i < routeSegments.Length; i++)
                {
                    var routeSegment = routeSegments[i];
                    var pathSegment = pathSegments[i];

                    if(routeSegment.StartsWith("{") && routeSegment.EndsWith("}"))
                    {
                        variablesCandidate.Add(routeSegment.Substring(1, routeSegment.Length - 2), pathSegment);
                    }
                    else if(routeSegment.Equals(pathSegment, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        candidateIsGood = false;
                        break;
                    }
                }

                if(candidateIsGood)
                {
                    page = routes[route]();
                    page.RouteVariables = new ReadOnlyDictionary<string, string>(variablesCandidate);
                    page.Path = path;
                    return true;
                }
            }

            page = null;
            return false;
        }

        private void FirePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
    }
}
