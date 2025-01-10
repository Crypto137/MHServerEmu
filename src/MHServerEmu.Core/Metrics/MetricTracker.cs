using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Core.Metrics
{
    /// <summary>
    /// Accumulates metric values over time.
    /// </summary>
    public class MetricTracker
    {
        private readonly CircularBuffer<float> _buffer;
        private float _min = float.MaxValue;
        private float _max = float.MinValue;
        private float _last = 0f;

        /// <summary>
        /// Constructs a new <see cref="MetricTracker"/> with the specified buffer size.
        /// </summary>
        public MetricTracker(int bufferSize)
        {
            _buffer = new(bufferSize);
        }

        /// <summary>
        /// Tracks a new <see cref="float"/> value.
        /// </summary>
        public void Track(float value)
        {
            _buffer.Add(value);
            _min = MathF.Min(_min, value);
            _max = MathF.Max(_max, value);
            _last = value;
        }

        /// <summary>
        /// Tracks a new <see cref="TimeSpan"/> value as a number of milliseconds.
        /// </summary>
        public void Track(TimeSpan value)
        {
            Track((float)value.TotalMilliseconds);
        }

        /// <summary>
        /// Returns a <see cref="ReportEntry"/> representing the current state of this <see cref="MetricTracker"/>.
        /// </summary>
        public ReportEntry AsReportEntry()
        {
            return new(this);
        }

        /// <summary>
        /// A snapshot of the state of a <see cref="MetricTracker"/>.
        /// </summary>
        public readonly struct ReportEntry
        {
            public float Min { get; }
            public float Max { get; }
            public float Average { get; }
            public float Median { get; }
            public float Last { get; }

            public ReportEntry(MetricTracker tracker)
            {
                Min = tracker._min;
                Max = tracker._max;
                Average = tracker._buffer.ToAverage();
                Median = tracker._buffer.ToMedian();
                Last = tracker._last;
            }

            public override string ToString()
            {
                return $"min={Min}, max={Max}, avg={Average}, mdn={Median}, last={Last}";
            }
        }
    }
}
