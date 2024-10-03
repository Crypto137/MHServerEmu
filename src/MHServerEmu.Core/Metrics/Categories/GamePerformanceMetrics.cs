using System.Runtime.InteropServices;
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

        private readonly TimeTracker _frameTimeTracker = new(1024);    // At 20 FPS this gives us about 51.2 seconds of data
        private readonly FloatTracker _catchUpFrameCountTracker = new(1024);
        private readonly TimeTracker _timeSkipTracker = new(1024);

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

                case GamePerformanceMetricEnum.CatchUpFrameCount:
                    _catchUpFrameCountTracker.Track(metricValue.FloatValue);
                    break;

                case GamePerformanceMetricEnum.TimeSkip:
                    _timeSkipTracker.Track(metricValue.TimeValue);
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
            public ReportFloatEntry CatchUpFrameCount { get; }
            public ReportTimeEntry TimeSkip { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                FrameTime = metrics._frameTimeTracker.AsReportEntry();
                CatchUpFrameCount = metrics._catchUpFrameCountTracker.AsReportEntry();
                TimeSkip = metrics._timeSkipTracker.AsReportEntry();
            }

            public override string ToString()
            {
                return $"{nameof(FrameTime)}: {FrameTime}\n{nameof(CatchUpFrameCount)}: {CatchUpFrameCount}\n{nameof(TimeSkip)}: {TimeSkip}";
            }
        }
    }
}
