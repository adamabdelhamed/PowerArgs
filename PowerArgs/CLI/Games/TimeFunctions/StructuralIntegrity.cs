using PowerArgs.Cli.Physics;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class StructuralIntegrity<T> : TimeFunction where T : SpacialElement
    {
        private Dictionary<T, ILocationF> structure = new Dictionary<T, ILocationF>();
        private List<T> matter;
        public StructuralIntegrity(List<T> matter)
        {
            this.matter = matter;
            structure.Add(matter[0], LocationF.Create(0, 0));

            for (var i = 1; i < matter.Count; i++)
            {
                var xDelta = matter[i].Left - matter[0].Left;
                var yDelta = matter[i].Top - matter[0].Top;
                structure.Add(matter[i], LocationF.Create(xDelta, yDelta));
            }
        }

        public override void Evaluate()
        {
            for (var i = 1; i < matter.Count; i++)
            {
                if(matter[i].Lifetime.IsExpired == false)
                {
                    matter[i].MoveTo(matter[0].Left + structure[matter[i]].Left, matter[0].Top + structure[matter[i]].Top);
                }
            }
        }
    }
}
