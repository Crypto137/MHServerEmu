using System.Text;
using MHServerEmu.Core.Logging;

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

        private readonly MetricTracker[] _trackers;

        public ulong GameId { get; }

        public GamePerformanceMetrics(ulong gameId)
        {
            GameId = gameId;

            _trackers = new MetricTracker[(int)GamePerformanceMetricEnum.NumGameMetrics];
            for (GamePerformanceMetricEnum metric = 0; metric < GamePerformanceMetricEnum.NumGameMetrics; metric++)
                _trackers[(int)metric] = new(NumSamples);
        }

        public bool Update(in GamePerformanceMetricValue gameMetricValue)
        {
            GamePerformanceMetricEnum metric = gameMetricValue.Metric;
            if (metric < 0 || metric >= GamePerformanceMetricEnum.NumGameMetrics)
                return Logger.WarnReturn(false, $"Update(): Metric {metric} is out of range");

            switch (metric)
            {
                case GamePerformanceMetricEnum.FrameTime:
                case GamePerformanceMetricEnum.FrameProcessServiceMessagesTime:
                case GamePerformanceMetricEnum.FrameTriggerEventsTime:
                case GamePerformanceMetricEnum.FrameLocomoteEntitiesTime:
                case GamePerformanceMetricEnum.FramePhysicsResolveEntitiesTime:
                case GamePerformanceMetricEnum.FrameProcessDeferredListsTime:
                case GamePerformanceMetricEnum.FrameSendAllPendingMessagesTime:
                case GamePerformanceMetricEnum.TimeSkip:
                    _trackers[(int)metric].Track(gameMetricValue.Value.TimeValue);
                    break;

                case GamePerformanceMetricEnum.CatchUpFrames:
                case GamePerformanceMetricEnum.ScheduledEventsPerUpdate:
                case GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate:
                case GamePerformanceMetricEnum.RemainingScheduledEvents:
                case GamePerformanceMetricEnum.EntityCount:
                case GamePerformanceMetricEnum.PlayerCount:
                    _trackers[(int)metric].Track(gameMetricValue.Value.FloatValue);
                    break;

                default:
                    Logger.WarnReturn(false, $"Update(): Unhandled metric {metric}");
                    break;
            }

            return true;
        }

        public Report GetReport()
        {
            return new(this);
        }

        private MetricTracker.ReportEntry GetReportEntryForMetric(GamePerformanceMetricEnum metric)
        {
            return _trackers[(int)metric].AsReportEntry();
        }

        public readonly struct Report
        {
            // TODO: Clean this up
            public MetricTracker.ReportEntry FrameTime { get; }
            public MetricTracker.ReportEntry FrameProcessServiceMessagesTime { get; }
            public MetricTracker.ReportEntry FrameTriggerEventsTime { get; }
            public MetricTracker.ReportEntry FrameLocomoteEntitiesTime { get; }
            public MetricTracker.ReportEntry FramePhysicsResolveEntitiesTime { get; }
            public MetricTracker.ReportEntry FrameProcessDeferredListsTime { get; }
            public MetricTracker.ReportEntry FrameSendAllPendingMessagesTime { get; }
            public MetricTracker.ReportEntry CatchUpFrames { get; }
            public MetricTracker.ReportEntry TimeSkip { get; }
            public MetricTracker.ReportEntry ScheduledEventsPerUpdate { get; }
            public MetricTracker.ReportEntry EventSchedulerFramesPerUpdate { get; }
            public MetricTracker.ReportEntry RemainingScheduledEvents { get; }
            public MetricTracker.ReportEntry EntityCount { get; }
            public MetricTracker.ReportEntry PlayerCount { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                FrameTime                       = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameTime);
                FrameProcessServiceMessagesTime = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameProcessServiceMessagesTime);
                FrameTriggerEventsTime          = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameTriggerEventsTime);
                FrameLocomoteEntitiesTime       = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameLocomoteEntitiesTime);
                FramePhysicsResolveEntitiesTime = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FramePhysicsResolveEntitiesTime);
                FrameProcessDeferredListsTime   = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameProcessDeferredListsTime);
                FrameSendAllPendingMessagesTime = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameSendAllPendingMessagesTime);
                CatchUpFrames                   = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.CatchUpFrames);
                TimeSkip                        = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.TimeSkip);
                ScheduledEventsPerUpdate        = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.ScheduledEventsPerUpdate);
                EventSchedulerFramesPerUpdate   = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate);
                RemainingScheduledEvents        = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.RemainingScheduledEvents);
                EntityCount                     = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.EntityCount);
                PlayerCount                     = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.PlayerCount);
            }

            public override string ToString()
            {
                StringBuilder sb = new();
                sb.AppendLine($"{nameof(FrameTime)}: {FrameTime}");
                sb.AppendLine($"{nameof(FrameProcessServiceMessagesTime)}: {FrameProcessServiceMessagesTime}");
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
