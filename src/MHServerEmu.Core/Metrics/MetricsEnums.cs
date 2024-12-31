namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json
    }

    public enum GamePerformanceMetricEnum
    {
        Invalid = -1,
        FrameTime,
        FrameTriggerEventsTime,
        FrameLocomoteEntitiesTime,
        FramePhysicsResolveEntitiesTime,
        FrameProcessDeferredListsTime,
        FrameSendAllPendingMessagesTime,
        CatchUpFrames,
        TimeSkip,
        ScheduledEventsPerUpdate,
        EventSchedulerFramesPerUpdate,
        RemainingScheduledEvents,
        EntityCount,
        PlayerCount,
        NumGameMetrics
    }
}
