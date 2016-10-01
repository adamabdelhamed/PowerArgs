using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class Path : List<PathElement>
    {
        public bool IsHighlighted
        {
            get
            {
                return this[0].IsHighlighted;
            }
            set
            {
                foreach (var element in this)
                {
                    element.IsHighlighted = value;
                }
            }
        }

        public Path(List<Location> points)
        {
            PathElement last = null;
            foreach(var point in points)
            {
                PathElement current = new PathElement() { Path = this, Bounds = new PowerArgs.Cli.Physics.Rectangle(point.X, point.Y, 1, 1) };
                current.Last = last;
                if(last != null)
                {
                    last.Next = current;
                }
                Add(current);
                last = current;
            }
        }

        public Path(List<PathElement> elements)
        {
            PathElement last = null;
            foreach (var element in elements)
            {
                element.Path = this;
                element.Last = last;
                if (last != null)
                {
                    last.Next = element;
                }
                Add(element);
                last = element;
            }
        }

        public Path(params Location[] points) : this(points.ToList()) { }
    }

    public class PathElement : Thing
    {
        public Path Path { get; set; }
        public PathElement Next { get; set; }
        public PathElement Last { get; set; }

        public bool IsHighlighted { get { return Observable.Get<bool>(); } set { Observable.Set(value); } }
    }

    [ThingBinding(typeof(PathElement))]
    public class PathElementRenderer : ThingRenderer
    {
        public PathElementRenderer()
        {
            this.TransparentBackground = true;

        }

        public override void OnBind()
        {
            (Thing as PathElement).SynchronizeForLifetime(nameof(PathElement.IsHighlighted), () =>
             {
                 bool highlighted = (Thing as PathElement).IsHighlighted;

                 if(Application != null)
                 {
                     Application.QueueAction(() =>
                     {
                         this.Background = highlighted ? ConsoleColor.Cyan : ConsoleColor.Black;
                         this.TransparentBackground = highlighted == false;
                     });
                 }
                 else
                 {
                     AddedToVisualTree.SubscribeForLifetime(() =>
                     {
                         this.Background = highlighted ? ConsoleColor.Cyan : ConsoleColor.Black;
                         this.TransparentBackground = highlighted == false;
                     }, this.LifetimeManager);
                 }
             }, this.LifetimeManager);
        }
    }
}
