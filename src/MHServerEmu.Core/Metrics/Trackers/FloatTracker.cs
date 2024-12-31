using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Metrics.Entries;

namespace MHServerEmu.Core.Metrics.Trackers
{
    /// <summary>
    /// Tracks <see cref="float"/> values.
    /// </summary>
    public class FloatTracker : IMetricTracker
    {
        private CircularBuffer<float> _buffer;
        private float _min = float.MaxValue;
        private float _max = float.MinValue;

        /// <summary>
        /// Constructs a new <see cref="FloatTracker"/> with the specified buffer size.
        /// </summary>
        public FloatTracker(int bufferSize)
        {
            _buffer = new(bufferSize);
        }

        /// <summary>
        /// Tracks a new <see cref="TimeSpan"/> value.
        /// </summary>
        public void Track(in MetricValue metricValue)
        {
            float value = metricValue.FloatValue;

            _buffer.Add(value);
            _min = MathF.Min(_min, value);
            _max = MathF.Max(_max, value);
        }

        /// <summary>
        /// Returns a <see cref="ReportTimeEntry"/> representing the current state of this <see cref="TimeTracker"/>.
        /// </summary>
        /// <returns></returns>
        public ReportFloatEntry AsReportEntry()
        {
            return new(_min, _max, _buffer.ToAverage(), _buffer.ToMedian());
        }
    }
}
