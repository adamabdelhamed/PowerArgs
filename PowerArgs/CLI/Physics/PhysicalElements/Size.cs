namespace PowerArgs.Cli.Physics
{
    public struct Size
    {
        public float W { get; set; }
        public float H { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Size) == false) return false;
            var other = (Size)obj;
            return W == other.W && H == other.H;
        }
    }
}
