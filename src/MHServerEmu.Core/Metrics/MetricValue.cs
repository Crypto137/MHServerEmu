using System.Runtime.InteropServices;

namespace MHServerEmu.Core.Metrics
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct MetricValue
    {
        [FieldOffset(0)]
        public readonly float FloatValue = default;
        [FieldOffset(0)]
        public readonly TimeSpan TimeValue = default;

        public MetricValue(float value)
        {
            FloatValue = value;
        }

        public MetricValue(TimeSpan value)
        {
            TimeValue = value;
        }
    }
}
