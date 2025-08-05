using System.Text;
using MHServerEmu.Core.Helpers;
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
                _trackers[(int)metric] = new(metric.ToString(), NumSamples);
        }

        public bool Update(in GamePerformanceMetricValue gameMetricValue)
        {
            GamePerformanceMetricEnum metric = gameMetricValue.Metric;
            if (metric < 0 || metric >= GamePerformanceMetricEnum.NumGameMetrics)
                return Logger.WarnReturn(false, $"Update(): Metric {metric} is out of range");

            switch (metric)
            {
                case GamePerformanceMetricEnum.UpdateTime:
                case GamePerformanceMetricEnum.FrameTime:
                    _trackers[(int)metric].Track(gameMetricValue.Value.TimeValue);
                    break;

                case GamePerformanceMetricEnum.ScheduledEventsPerUpdate:
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

        public readonly struct Report : IHtmlDataStructure
        {
            public MetricTracker.ReportEntry UpdateTime { get; }
            public MetricTracker.ReportEntry FrameTime { get; }
            public MetricTracker.ReportEntry ScheduledEventsPerUpdate { get; }
            public MetricTracker.ReportEntry EntityCount { get; }
            public MetricTracker.ReportEntry PlayerCount { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                UpdateTime                      = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.UpdateTime);
                FrameTime                       = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.FrameTime);
                ScheduledEventsPerUpdate        = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.ScheduledEventsPerUpdate);
                EntityCount                     = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.EntityCount);
                PlayerCount                     = metrics.GetReportEntryForMetric(GamePerformanceMetricEnum.PlayerCount);
            }

            public override string ToString()
            {
                StringBuilder sb = new();
                sb.AppendLine($"{nameof(UpdateTime)}: {UpdateTime}");
                sb.AppendLine($"{nameof(FrameTime)}: {FrameTime}");
                sb.AppendLine($"{nameof(ScheduledEventsPerUpdate)}: {ScheduledEventsPerUpdate}");
                sb.AppendLine($"{nameof(EntityCount)}: {EntityCount}");
                sb.AppendLine($"{nameof(PlayerCount)}: {PlayerCount}");
                return sb.ToString();
            }

            public void BuildHtml(StringBuilder sb)
            {
                HtmlBuilder.BeginTable(sb);

                HtmlBuilder.AppendTableRow(sb, "Metric", "Avg", "Mdn", "Last", "Min", "Max");

                HtmlBuilder.AppendDataStructure(sb, UpdateTime);
                HtmlBuilder.AppendDataStructure(sb, FrameTime);
                HtmlBuilder.AppendDataStructure(sb, ScheduledEventsPerUpdate);
                HtmlBuilder.AppendDataStructure(sb, EntityCount);
                HtmlBuilder.AppendDataStructure(sb, PlayerCount);

                HtmlBuilder.EndTable(sb);
            }
        }
    }
}
