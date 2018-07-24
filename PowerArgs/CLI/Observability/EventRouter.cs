using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PowerArgs.Cli
{
    public class RoutedEvent<T>
    {
        public string Path { get; set; }
        public string Route { get; set; }
        public IReadOnlyDictionary<string,string> RouteVariables { get; set; }
        public T Data { get; set; }
    }

    /// <summary>
    /// A router that can route events based on paths and variables
    /// </summary>
    /// <typeparam name="T">The event data object type</typeparam>
    public class EventRouter<T>
    {
        private Dictionary<string, Event<RoutedEvent<T>>> routes = new Dictionary<string, Event<RoutedEvent<T>>>();

        /// <summary>
        /// Fires an event with the given id and data
        /// </summary>
        /// <param name="path">the event id</param>
        /// <param name="data">the event data to send to subscribers</param>
        public void Fire(string path, T data)
        {
            path = path.ToLower();
            if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);

            if (path == "")
            {
                return;
            }

            var pathSegments = path.Split('/');
            foreach (var route in routes.Keys)
            {
                var variablesCandidate = new Dictionary<string, string>();
                var routeSegments = route.Split('/');

                if (pathSegments.Length == routeSegments.Length)
                {
                    // the path and route has the same # of segments
                }
                else if (routeSegments.Length > pathSegments.Length)
                {
                    // path can't match route because the path is too short, skip it
                    continue;
                }
                else if (pathSegments.Length > routeSegments.Length && route.Contains("{*}"))
                {
                    // this route supports dynamic routing so it's ok to keep checking if the path is longer than the route
                }
                else
                {
                    continue;
                }

                bool candidateIsGood = true;
                for (int i = 0; i < routeSegments.Length; i++)
                {
                    var routeSegment = routeSegments[i];
                    var pathSegment = pathSegments[i];

                    if (routeSegment.StartsWith("{") && routeSegment.EndsWith("}"))
                    {
                        var variableName = routeSegment.Substring(1, routeSegment.Length - 2);

                        if (variableName == "*")
                        {
                            var restOfPathValue = "";
                            for (int j = i; j < pathSegments.Length; j++)
                            {
                                restOfPathValue += pathSegments[j] + "/";
                            }
                            restOfPathValue = restOfPathValue.Substring(0, restOfPathValue.Length - 1);
                            variablesCandidate.Add(variableName, restOfPathValue);
                            break;
                        }
                        else
                        {
                            variablesCandidate.Add(variableName, pathSegment);
                        }
                    }
                    else if (routeSegment.Equals(pathSegment, StringComparison.InvariantCultureIgnoreCase) == false)
                    {
                        candidateIsGood = false;
                        break;
                    }
                }

                if (candidateIsGood)
                {
                    var ev = routes[route];
                    var args = new RoutedEvent<T>()
                    {
                        Data = data,
                        Path = path,
                        Route = route,
                        RouteVariables = new ReadOnlyDictionary<string, string>(variablesCandidate)
                    };
                    ev.Fire(args);
                }
            }
        }

        /// <summary>
        /// Subscribes to the given event
        /// </summary>
        /// <param name="route">the event to subscribe to</param>
        /// <param name="handler">the event handler</param>
        /// <returns>a disposable that can be disposed when you want to stop subscribing</returns>
        public IDisposable SubscribeUnmanaged(string route, Action<RoutedEvent<T>> handler) => GetOrAddRoutedEvent(route).SubscribeUnmanaged(handler);

        /// <summary>
        /// Subscribes to the given event for the given lifetime
        /// </summary>
        /// <param name="route">the event to subscribe to</param>
        /// <param name="handler">the event handler</param>
        /// <param name="lifetimeManager">defines the lifetime of the subscription</param>
        public void SubscribeForLifetime(string route, Action<RoutedEvent<T>> handler, ILifetimeManager lifetimeManager) => GetOrAddRoutedEvent(route).SubscribeForLifetime(handler, lifetimeManager);

        /// <summary>
        /// Subscribes to the given event for at most one notification
        /// </summary>
        /// <param name="route">the event to subscribe to</param>
        /// <param name="handler">the event handler</param>
        public void SubscribeOnce(string route, Action<RoutedEvent<T>> handler) => GetOrAddRoutedEvent(route).SubscribeOnce(handler);

        private Event<RoutedEvent<T>> GetOrAddRoutedEvent(string route)
        {
            route = route.ToLower();
            if (routes.TryGetValue(route, out Event<RoutedEvent<T>> innerEvent) == false)
            {
                innerEvent = new Event<RoutedEvent<T>>();
                routes.Add(route, innerEvent);
            }
            return innerEvent;
        }
    }
}
