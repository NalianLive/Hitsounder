using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Linq;

namespace Hitsounder
{
    public interface ISampleManager
    {
        ISampleProvider GetSampleProvider(short type, short index);
    }

    public class OutputGenerator
    {
        public const double OutputSampleRate = 44100;

        public ISampleManager SampleManager { get; set; }

        public OutputGenerator(ISampleManager sampleManager)
        {
            SampleManager = sampleManager;
        }

        public ISampleProvider Generate(double songLength, TimingPoint[] timingPoints)
        {
            var offsetSampleProviders = new OffsetSampleProvider[timingPoints.Length];

            foreach (var (timingPoint, index) in timingPoints.Select((item, index) => (item, index)))
            {
                offsetSampleProviders[index] = new OffsetSampleProvider(SampleManager.GetSampleProvider(timingPoint.SampleType, timingPoint.SampleIndex))
                {
                    DelayBy = TimeSpan.FromMilliseconds(timingPoint.Position),
                };
            }

            return new MixingSampleProvider(offsetSampleProviders);
        }
    }
}
