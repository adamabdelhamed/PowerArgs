namespace PowerArgs.Cli.Physics
{
    public class RollingAverage
    {
        private double[] samples;
        private int index;
        private bool isWindowFull;

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
