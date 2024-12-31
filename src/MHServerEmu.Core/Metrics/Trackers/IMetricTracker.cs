namespace MHServerEmu.Core.Metrics.Trackers
{
    internal interface IMetricTracker
    {
        public void Track(in MetricValue metricValue);
    }
}
