namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json,
        Html,
    }

    public enum GamePerformanceMetricEnum
    {
        Invalid = -1,
        UpdateTime,
        FrameTime,
        ScheduledEventsPerUpdate,
        EntityCount,
        PlayerCount,
        NumGameMetrics
    }
}
