using System;
using System.Collections.Generic;

namespace PowerArgs.Cli.Physics
{
    public class Rectangle
    {
        public Location Location { get; set; }
        public Size Size { get; set; }

        public float X { get { return Location.X; } }
        public float Y { get { return Location.Y; } }
        public float W { get { return Size.W; } }
        public float H { get { return Size.H; } }

        public Rectangle(float x, float y, float w, float h)
        {
            this.Location = new Location() { X = x, Y = y };
            this.Size = new Size() { W = w, H = h };
        }

        public void MoveTo(Location newLocation)
        {
            this.Location = newLocation;
        }

        public void MoveBy(float x, float y)
        {
            this.Location = new Location() { X = this.Location.X + x, Y = this.Location.Y + y };
        }

        public void Resize(Size newSize)
        {
            this.Size = newSize;
        }

        public void GrowBy(int w, int h)
        {
            this.Size = new Size() { W = this.Size.W + w, H = this.Size.H + h };
        }

        public bool HitsVertically(Rectangle other)
        {
            float myBottom = Location.Y + Size.H;
            float myTop = Location.Y;

            float otherBottom = other.Location.Y + other.Size.H;
            float otherTop = other.Location.Y;

            bool ret = (myBottom >= otherBottom && myTop <= otherBottom) ||
                    (myBottom <= otherBottom && myBottom >= otherTop);
            return ret;
        }

        public bool HitsHorizontally(Rectangle other)
        {
            float myLeft = Location.X;
            float myRight = Location.X + Size.W;

            float otherLeft = other.Location.X;
            float otherRight = other.Location.X + other.Size.W;

            bool ret = (myLeft <= otherLeft && myRight >= otherLeft) ||
                    (myLeft >= otherLeft && myLeft <= otherRight);
            return ret;
        }

        public Direction GetHitDirection(Rectangle other)
        {
            float rightProximity = Math.Abs(other.Location.X - (Location.X + Size.W));
            float leftProximity = Math.Abs(other.Location.X + other.Size.W - Location.X);
            float topProximity = Math.Abs(other.Location.Y + other.Size.H - Location.Y);
            float bottomProximity = Math.Abs(other.Location.Y - (Location.Y + Size.H));

            rightProximity = ((int)((rightProximity * 10) + .5f)) / 10f;
            leftProximity = ((int)((leftProximity * 10) + .5f)) / 10f;
            topProximity = ((int)((topProximity * 10) + .5f)) / 10f;
            bottomProximity = ((int)((bottomProximity * 10) + .5f)) / 10f;

            if (leftProximity == topProximity) return Direction.UpLeft;
            if (rightProximity == topProximity) return Direction.UpRight;
            if (leftProximity == bottomProximity) return Direction.DownLeft;
            if (rightProximity == bottomProximity) return Direction.DownRight;

            List<KeyValuePair<Direction, float>> items = new List<KeyValuePair<Direction, float>>();
            items.Add(new KeyValuePair<Direction, float>(Direction.Right, rightProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Left, leftProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Up, topProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Down, bottomProximity));
            items.Sort(new DirSorter());
            return items[0].Key;

        }

        public class DirSorter : IComparer<KeyValuePair<Direction, float>>
        {
            public int Compare(KeyValuePair<Direction, float> x, KeyValuePair<Direction, float> y)
            {
                return x.Value.CompareTo(y.Value);
            }
        }

        public bool Hits(Rectangle other)
        {
            return HitsHorizontally(other) && HitsVertically(other);
        }

        public bool Contains(Rectangle other)
        {
            float myBottom = Location.Y + Size.H;
            float myTop = Location.Y;

            float otherBottom = other.Location.Y + other.Size.H;
            float otherTop = other.Location.Y;

            float myLeft = Location.X;
            float myRight = Location.X + Size.W;

            float otherLeft = other.Location.X;
            float otherRight = other.Location.X + other.Size.W;


            bool containsVertically = otherTop >= myTop && otherBottom <= myBottom;
            bool containsHorizontally = otherLeft >= myLeft && otherRight <= myRight;
            return containsVertically && containsHorizontally;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj as Rectangle == null) return false;
            var other = obj as Rectangle;
            return Size.Equals(other.Size) && Location.Equals(other.Location);
        }
    }
}
