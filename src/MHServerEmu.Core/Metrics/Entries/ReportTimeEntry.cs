namespace MHServerEmu.Core.Metrics.Entries
{
    public readonly struct ReportTimeEntry
    {
        public float Min { get; }
        public float Max { get; }
        public float Average { get; }
        public float Median { get; }

        public ReportTimeEntry(float min, float max, float average, float median)
        {
            Min = min;
            Max = max;
            Average = average;
            Median = median;
        }

        public override string ToString()
        {
            return $"min={Min} ms, max={Max} ms, avg={Average} ms, mdn={Median} ms";
        }
    }
}
