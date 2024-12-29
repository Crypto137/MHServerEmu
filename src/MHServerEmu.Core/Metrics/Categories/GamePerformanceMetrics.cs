using System.Runtime.InteropServices;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Metrics.Entries;
using MHServerEmu.Core.Metrics.Trackers;

namespace MHServerEmu.Core.Metrics.Categories
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct GamePerformanceMetricValue
    {
        [FieldOffset(0)]
        public readonly GamePerformanceMetricEnum Metric = default;
        [FieldOffset(4)]
        public readonly float FloatValue = default;
        [FieldOffset(4)]
        public readonly TimeSpan TimeValue = default;

        public GamePerformanceMetricValue(GamePerformanceMetricEnum metric, float value)
        {
            Metric = metric;
            FloatValue = value;
        }

        public GamePerformanceMetricValue(GamePerformanceMetricEnum metric, TimeSpan value)
        {
            Metric = metric;
            TimeValue = value;
        }
    }

    public class GamePerformanceMetrics
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // At 20 FPS this gives us about 51.2 seconds of data
        private const int NumSamples = 1024;

        private readonly TimeTracker _frameTimeTracker = new(NumSamples);
        private readonly TimeTracker _frameTriggerEventsTimeTracker = new(NumSamples);
        private readonly TimeTracker _frameLocomoteEntitiesTimeTracker = new(NumSamples);
        private readonly TimeTracker _framePhysicsResolveEntitiesTimeTracker = new(NumSamples);
        private readonly TimeTracker _frameProcessDeferredListsTimeTracker = new(NumSamples);
        private readonly TimeTracker _frameSendAllPendingMessagesTime = new(NumSamples);

        private readonly FloatTracker _catchUpFrameCountTracker = new(NumSamples);
        private readonly TimeTracker _timeSkipTracker = new(NumSamples);

        private readonly FloatTracker _scheduledEventsPerUpdateTracker = new(NumSamples);
        private readonly FloatTracker _eventSchedulerFramesPerUpdate = new(NumSamples);
        private readonly FloatTracker _remainingScheduledEventsTracker = new(NumSamples);

        private readonly FloatTracker _entityCountTracker = new(NumSamples);

        public ulong GameId { get; }

        public GamePerformanceMetrics(ulong gameId)
        {
            GameId = gameId;
        }

        public void Update(in GamePerformanceMetricValue metricValue)
        {
            // add more data here

            switch (metricValue.Metric)
            {
                case GamePerformanceMetricEnum.FrameTime:
                    _frameTimeTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.FrameTriggerEventsTime:
                    _frameTriggerEventsTimeTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.FrameLocomoteEntitiesTime:
                    _frameLocomoteEntitiesTimeTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.FramePhysicsResolveEntitiesTime:
                    _framePhysicsResolveEntitiesTimeTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.FrameProcessDeferredListsTime:
                    _frameProcessDeferredListsTimeTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.FrameSendAllPendingMessagesTime:
                    _frameSendAllPendingMessagesTime.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.CatchUpFrames:
                    _catchUpFrameCountTracker.Track(metricValue.FloatValue);
                    break;

                case GamePerformanceMetricEnum.TimeSkip:
                    _timeSkipTracker.Track(metricValue.TimeValue);
                    break;

                case GamePerformanceMetricEnum.ScheduledEventsPerUpdate:
                    _scheduledEventsPerUpdateTracker.Track(metricValue.FloatValue);
                    break;

                case GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate:
                    _eventSchedulerFramesPerUpdate.Track(metricValue.FloatValue);
                    break;

                case GamePerformanceMetricEnum.RemainingScheduledEvents:
                    _remainingScheduledEventsTracker.Track(metricValue.FloatValue);
                    break;

                case GamePerformanceMetricEnum.EntityCount:
                    _entityCountTracker.Track(metricValue.FloatValue);
                    break;

                default:
                    Logger.Warn($"Update(): Unknown game performance metric {metricValue.Metric}");
                    break;
            }
        }

        public Report GetReport()
        {
            return new(this);
        }

        public readonly struct Report
        {
            public ReportTimeEntry FrameTime { get; }
            public ReportTimeEntry FrameTriggerEventsTime { get; }
            public ReportTimeEntry FrameLocomoteEntitiesTime { get; }
            public ReportTimeEntry FramePhysicsResolveEntitiesTime { get; }
            public ReportTimeEntry FrameProcessDeferredListsTime { get; }
            public ReportTimeEntry FrameSendAllPendingMessagesTime { get; }
            public ReportFloatEntry CatchUpFrames { get; }
            public ReportTimeEntry TimeSkip { get; }
            public ReportFloatEntry ScheduledEventsPerUpdate { get; }
            public ReportFloatEntry EventSchedulerFramesPerUpdate { get; }
            public ReportFloatEntry RemainingScheduledEvents { get; }
            public ReportFloatEntry EntityCount { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                FrameTime = metrics._frameTimeTracker.AsReportEntry();
                FrameTriggerEventsTime = metrics._frameTriggerEventsTimeTracker.AsReportEntry();
                FrameLocomoteEntitiesTime = metrics._frameLocomoteEntitiesTimeTracker.AsReportEntry();
                FramePhysicsResolveEntitiesTime = metrics._framePhysicsResolveEntitiesTimeTracker.AsReportEntry();
                FrameProcessDeferredListsTime = metrics._frameProcessDeferredListsTimeTracker.AsReportEntry();
                FrameSendAllPendingMessagesTime = metrics._frameSendAllPendingMessagesTime.AsReportEntry();
                CatchUpFrames = metrics._catchUpFrameCountTracker.AsReportEntry();
                TimeSkip = metrics._timeSkipTracker.AsReportEntry();
                ScheduledEventsPerUpdate = metrics._scheduledEventsPerUpdateTracker.AsReportEntry();
                EventSchedulerFramesPerUpdate = metrics._eventSchedulerFramesPerUpdate.AsReportEntry();
                RemainingScheduledEvents = metrics._remainingScheduledEventsTracker.AsReportEntry();
                EntityCount = metrics._entityCountTracker.AsReportEntry();
            }

            public override string ToString()
            {
                StringBuilder sb = new();
                sb.AppendLine($"{nameof(FrameTime)}: {FrameTime}");
                sb.AppendLine($"{nameof(FrameTriggerEventsTime)}: {FrameTriggerEventsTime}");
                sb.AppendLine($"{nameof(FrameLocomoteEntitiesTime)}: {FrameLocomoteEntitiesTime}");
                sb.AppendLine($"{nameof(FramePhysicsResolveEntitiesTime)}: {FramePhysicsResolveEntitiesTime}");
                sb.AppendLine($"{nameof(FrameProcessDeferredListsTime)}: {FrameProcessDeferredListsTime}");
                sb.AppendLine($"{nameof(FrameSendAllPendingMessagesTime)}: {FrameSendAllPendingMessagesTime}");
                sb.AppendLine($"{nameof(CatchUpFrames)}: {CatchUpFrames}");
                sb.AppendLine($"{nameof(TimeSkip)}: {TimeSkip}");
                sb.AppendLine($"{nameof(ScheduledEventsPerUpdate)}: {ScheduledEventsPerUpdate}");
                sb.AppendLine($"{nameof(EventSchedulerFramesPerUpdate)}: {EventSchedulerFramesPerUpdate}");
                sb.AppendLine($"{nameof(RemainingScheduledEvents)}: {RemainingScheduledEvents}");
                sb.AppendLine($"{nameof(EntityCount)}: {EntityCount}");
                return sb.ToString();
            }
        }
    }
}
