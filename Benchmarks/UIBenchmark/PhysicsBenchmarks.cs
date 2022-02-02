using BenchmarkDotNet.Attributes;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmarks
{
    public class PhysicsBenchmarks
    {
        private HitDetectionOptions options;
        public PhysicsBenchmarks()
        {
            var r = new Random(100);
            options = new HitDetectionOptions()
            {
                Obstacles = new RectF[100],
                MovingObject = new RectF(r.Next(0,100), r.Next(0, 100), r.Next(0, 100), r.Next(0, 100)),
                Angle = r.Next(0,360),
            };

            for(var i = 0; i < options.Obstacles.Length; i++)
            {
                options.Obstacles[i] = new RectF(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
            }
        }

        [Benchmark]
        public void BenchHitDetection()
        {
            HitDetection.PredictHit(options);
        }
    }
}
