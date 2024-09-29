using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Metrics.Entries;
using MHServerEmu.Core.Metrics.Trackers;

namespace MHServerEmu.Core.Metrics.Categories
{
    public class MemoryMetrics
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private long _gcIndex = 0;
        private long _totalCommittedBytes;
        private readonly FloatTracker _pauseTimePercentageTracker = new(512);
        private readonly TimeTracker _pauseDurationTracker = new(512);

        public void Update()
        {
            // Get newest memory info and see if we are up to date
            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

            long index = memoryInfo.Index;
            if (_gcIndex >= index)
                return;

            _gcIndex = memoryInfo.Index;
            _totalCommittedBytes = memoryInfo.TotalCommittedBytes;
            _pauseTimePercentageTracker.Track((float)memoryInfo.PauseTimePercentage);
            _pauseDurationTracker.Track(memoryInfo.PauseDurations[0]);
        }

        public Report GetReport()
        {
            return new(this);
        }

        public readonly struct Report
        {
            public long GCIndex { get; }
            public long TotalCommittedBytes { get; }
            public ReportFloatEntry PauseTimePercentage { get; }
            public ReportTimeEntry PauseDuration { get; }

            public Report(MemoryMetrics metrics)
            {
                GCIndex = metrics._gcIndex;
                TotalCommittedBytes = metrics._totalCommittedBytes;
                PauseTimePercentage = metrics._pauseTimePercentageTracker.AsReportEntry();
                PauseDuration = metrics._pauseDurationTracker.AsReportEntry();
            }

            public override string ToString()
            {
                StringBuilder sb = new();

                sb.AppendLine($"{nameof(GCIndex)}: {GCIndex}");
                sb.AppendLine($"{nameof(TotalCommittedBytes)}: {TotalCommittedBytes}");
                sb.AppendLine($"{nameof(PauseTimePercentage)}: {PauseTimePercentage}");
                sb.AppendLine($"{nameof(PauseDuration)}: {PauseDuration}");

                return sb.ToString();
            }
        }
    }
}
