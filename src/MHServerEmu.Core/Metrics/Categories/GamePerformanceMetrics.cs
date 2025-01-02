using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Metrics.Entries;
using MHServerEmu.Core.Metrics.Trackers;

namespace MHServerEmu.Core.Metrics.Categories
{
    public readonly struct GamePerformanceMetricValue
    {
        public readonly GamePerformanceMetricEnum Metric = GamePerformanceMetricEnum.Invalid;
        public readonly MetricValue Value = default;

        public GamePerformanceMetricValue(GamePerformanceMetricEnum metric, float value)
        {
            Metric = metric;
            Value = new(value);
        }

        public GamePerformanceMetricValue(GamePerformanceMetricEnum metric, TimeSpan value)
        {
            Metric = metric;
            Value = new(value);
        }
    }

    public class GamePerformanceMetrics
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // At 20 FPS this gives us about 51.2 seconds of data
        private const int NumSamples = 1024;

        private readonly IMetricTracker[] _trackers;

        public ulong GameId { get; }

        public GamePerformanceMetrics(ulong gameId)
        {
            GameId = gameId;

            _trackers = new IMetricTracker[(int)GamePerformanceMetricEnum.NumGameMetrics];
            for (GamePerformanceMetricEnum metric = 0; metric < GamePerformanceMetricEnum.NumGameMetrics; metric++)
                _trackers[(int)metric] = CreateTrackerForMetric(metric);
        }

        public bool Update(in GamePerformanceMetricValue gameMetricValue)
        {
            GamePerformanceMetricEnum metric = gameMetricValue.Metric;
            if (metric < 0 || metric >= GamePerformanceMetricEnum.NumGameMetrics)
                return Logger.WarnReturn(false, $"Update(): Metric {metric} is out of range");

            _trackers[(int)metric].Track(gameMetricValue.Value);
            return true;
        }

        public Report GetReport()
        {
            return new(this);
        }

        private static IMetricTracker CreateTrackerForMetric(GamePerformanceMetricEnum metric)
        {
            switch (metric)
            {
                case GamePerformanceMetricEnum.FrameTime:
                case GamePerformanceMetricEnum.FrameTriggerEventsTime:
                case GamePerformanceMetricEnum.FrameLocomoteEntitiesTime:
                case GamePerformanceMetricEnum.FramePhysicsResolveEntitiesTime:
                case GamePerformanceMetricEnum.FrameProcessDeferredListsTime:
                case GamePerformanceMetricEnum.FrameSendAllPendingMessagesTime:
                case GamePerformanceMetricEnum.TimeSkip:
                    return new TimeTracker(NumSamples);

                case GamePerformanceMetricEnum.CatchUpFrames:
                case GamePerformanceMetricEnum.ScheduledEventsPerUpdate:
                case GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate:
                case GamePerformanceMetricEnum.RemainingScheduledEvents:
                case GamePerformanceMetricEnum.EntityCount:
                case GamePerformanceMetricEnum.PlayerCount:
                    return new FloatTracker(NumSamples);

                default:
                    return Logger.WarnReturn(new FloatTracker(NumSamples), $"GetTrackerForMetric(): Unhandled metric {metric}");
            }
        }

        private T GetTrackerForMetric<T>(GamePerformanceMetricEnum metric) where T: IMetricTracker
        {
            return (T)_trackers[(int)metric];
        }

        public readonly struct Report
        {
            // TODO: Clean this up
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
            public ReportFloatEntry PlayerCount { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                FrameTime                       = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FrameTime).AsReportEntry();
                FrameTriggerEventsTime          = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FrameTriggerEventsTime).AsReportEntry();
                FrameLocomoteEntitiesTime       = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FrameLocomoteEntitiesTime).AsReportEntry();
                FramePhysicsResolveEntitiesTime = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FramePhysicsResolveEntitiesTime).AsReportEntry();
                FrameProcessDeferredListsTime   = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FrameProcessDeferredListsTime).AsReportEntry();
                FrameSendAllPendingMessagesTime = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.FrameSendAllPendingMessagesTime).AsReportEntry();
                CatchUpFrames                   = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.CatchUpFrames).AsReportEntry();
                TimeSkip                        = metrics.GetTrackerForMetric<TimeTracker>(GamePerformanceMetricEnum.TimeSkip).AsReportEntry();
                ScheduledEventsPerUpdate        = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.ScheduledEventsPerUpdate).AsReportEntry();
                EventSchedulerFramesPerUpdate   = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate).AsReportEntry();
                RemainingScheduledEvents        = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.RemainingScheduledEvents).AsReportEntry();
                EntityCount                     = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.EntityCount).AsReportEntry();
                PlayerCount                     = metrics.GetTrackerForMetric<FloatTracker>(GamePerformanceMetricEnum.PlayerCount).AsReportEntry();
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
                sb.AppendLine($"{nameof(PlayerCount)}: {PlayerCount}");
                return sb.ToString();
            }
        }
    }
}
