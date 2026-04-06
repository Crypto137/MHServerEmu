namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json,
    }

    public enum GamePerformanceMetricEnum
    {
        Invalid = -1,
        ProcessingTime,
        FrameTime,
        ScheduledEventsPerUpdate,
        EntityCount,
        PlayerCount,
        NumGameMetrics
    }
}
