namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json
    }

    public enum GamePerformanceMetricEnum
    {
        Invalid,
        FrameTime,
        CatchUpFrameCount,
        TimeSkip
    }
}
