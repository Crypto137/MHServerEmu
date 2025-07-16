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
        FrameTime,
        FrameProcessServiceMessagesTime,
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
