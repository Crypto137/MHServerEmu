using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Metrics.Entries;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Metrics.Trackers
{
    /// <summary>
    /// Tracks <see cref="TimeSpan"/> values.
    /// </summary>
    public class TimeTracker : IMetricTracker
    {
        private CircularBuffer<TimeSpan> _buffer;
        private TimeSpan _min = TimeSpan.MaxValue;
        private TimeSpan _max = TimeSpan.MinValue;

        /// <summary>
        /// Constructs a new <see cref="TimeTracker"/> with the specified buffer size.
        /// </summary>
        public TimeTracker(int bufferSize)
        {
            _buffer = new(bufferSize);
        }

        /// <summary>
        /// Tracks a new <see cref="TimeSpan"/> value.
        /// </summary>
        public void Track(in MetricValue metricValue)
        {
            TimeSpan time = metricValue.TimeValue;

            _buffer.Add(time);
            _min = Clock.Min(_min, time);
            _max = Clock.Max(_max, time);
        }

        /// <summary>
        /// Returns a <see cref="ReportTimeEntry"/> representing the current state of this <see cref="TimeTracker"/>.
        /// </summary>
        /// <returns></returns>
        public ReportTimeEntry AsReportEntry()
        {
            float min = (float)_min.TotalMilliseconds;
            float max = (float)_max.TotalMilliseconds;
            float average = (float)_buffer.ToAverage().TotalMilliseconds;
            float median = (float)_buffer.ToMedian().TotalMilliseconds;

            return new(min, max, average, median);
        }
    }
}
