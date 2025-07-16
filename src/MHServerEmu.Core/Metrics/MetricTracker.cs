using System.Text;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;

namespace MHServerEmu.Core.Metrics
{
    /// <summary>
    /// Accumulates metric values over time.
    /// </summary>
    public class MetricTracker
    {
        private readonly string _name;
        private readonly CircularBuffer<float> _buffer;
        private float _last = 0f;
        private float _min = float.MaxValue;
        private float _max = float.MinValue;

        /// <summary>
        /// Constructs a new <see cref="MetricTracker"/> with the specified buffer size.
        /// </summary>
        public MetricTracker(string name, int bufferSize)
        {
            _name = name;
            _buffer = new(bufferSize);
        }

        public override string ToString()
        {
            return _name;
        }

        /// <summary>
        /// Tracks a new <see cref="float"/> value.
        /// </summary>
        public void Track(float value)
        {
            _buffer.Add(value);
            _last = value;
            _min = MathF.Min(_min, value);
            _max = MathF.Max(_max, value);
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
        public readonly struct ReportEntry : IHtmlDataStructure
        {
            public string Name { get; }
            public float Average { get; }
            public float Median { get; }
            public float Last { get; }
            public float Min { get; }
            public float Max { get; }

            public ReportEntry(MetricTracker tracker)
            {
                Name = tracker._name;
                Average = tracker._buffer.ToAverage();
                Median = tracker._buffer.ToMedian();
                Last = tracker._last;
                Min = tracker._min;
                Max = tracker._max;
            }

            public override string ToString()
            {
                return $"avg={Average}, mdn={Median}, last={Last}, min={Min}, max={Max}";
            }

            public void BuildHtml(StringBuilder sb)
            {
                HtmlBuilder.AppendTableRow(sb,
                    Name,
                    Average.ToString("0.00"),
                    Median.ToString("0.00"),
                    Last.ToString("0.00"),
                    Min != float.MaxValue ? Min.ToString("0.00") : "0.00",
                    Max != float.MinValue ? Max.ToString("0.00") : "0.00");
            }
        }
    }
}
