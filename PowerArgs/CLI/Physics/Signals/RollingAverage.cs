using System;

namespace PowerArgs.Cli.Physics
{
    public class RollingAverage
    {
        private double[] samples;
        private int index;
        private bool isWindowFull;

        public double[] Samples => samples;

        public bool IsWindowFull => isWindowFull;

        public double Average
        {
            get
            {
                var total = 0.0;
                var count = 0;
                lock (samples)
                {
                    for (var i = 0; i < samples.Length; i++)
                    {
                        if (isWindowFull == false && i == index) break;

                        total += samples[i];
                        count++;
                    }
                }

                return total / count;
            }
        }

        public double Min
        {
            get
            {
                var min = double.MaxValue;
                lock (samples)
                {
                    for (var i = 0; i < samples.Length; i++)
                    {
                        if (isWindowFull == false && i == index) break;

                        if(samples[i] < min)
                        {
                            min = samples[i];
                        }
                    }
                }

                return min;
            }
        }

        public double Max
        {
            get
            {
                var max = double.MinValue;
                lock (samples)
                {
                    for (var i = 0; i < samples.Length; i++)
                    {
                        if (isWindowFull == false && i == index) break;

                        if (samples[i] > max)
                        {
                            max = samples[i];
                        }
                    }
                }

                return max;
            }
        }

        public double Percentile(double p)
        {
            lock (samples)
            {
                if (isWindowFull == false) return float.NaN;
                Array.Sort(samples);
                var i = (int)Geometry.Round((samples.Length - 1) * p);
                return samples[i];
            }
        }

        public RollingAverage(int sampleWindow)
        {
            samples = new double[sampleWindow];
        }

        public void AddSample(double sample)
        {
            lock (samples)
            {
                samples[index++] = sample;
                if (index == samples.Length)
                {
                    index = 0;
                    isWindowFull = true;
                }
            }
        }
    }
}
