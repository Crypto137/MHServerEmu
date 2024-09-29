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
        private long[] _gcCounts = new long[GC.MaxGeneration + 1];
        private long _totalCommittedBytes;
        private double _pauseTimePercentage;
        private readonly TimeTracker _pauseDurationTracker = new(512);

        public void Update()
        {
            // Get newest memory info and see if we are up to date
            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();

            long index = memoryInfo.Index;
            if (_gcIndex >= index)
                return;

            _gcIndex = memoryInfo.Index;
            _gcCounts[memoryInfo.Generation]++;
            _totalCommittedBytes = memoryInfo.TotalCommittedBytes;
            _pauseTimePercentage = memoryInfo.PauseTimePercentage;
            _pauseDurationTracker.Track(memoryInfo.PauseDurations[0]);
        }

        public Report GetReport()
        {
            return new(this);
        }

        public readonly struct Report
        {
            public long GCIndex { get; }
            public long GCCountGen0 { get; }
            public long GCCountGen1 { get; }
            public long GCCountGen2 { get; }
            public long TotalCommittedBytes { get; }
            public double PauseTimePercentage { get; }
            public ReportTimeEntry PauseDuration { get; }

            public Report(MemoryMetrics metrics)
            {
                GCIndex = metrics._gcIndex;
                GCCountGen0 = metrics._gcCounts[0];
                GCCountGen1 = metrics._gcCounts[1];
                GCCountGen2 = metrics._gcCounts[2];
                TotalCommittedBytes = metrics._totalCommittedBytes;
                PauseTimePercentage = metrics._pauseTimePercentage;
                PauseDuration = metrics._pauseDurationTracker.AsReportEntry();
            }

            public override string ToString()
            {
                StringBuilder sb = new();

                sb.AppendLine($"{nameof(GCIndex)}: {GCIndex}");
                sb.AppendLine($"GCCounts: Gen0={GCCountGen0}, Gen1={GCCountGen1}, Gen2={GCCountGen2}");
                sb.AppendLine($"{nameof(TotalCommittedBytes)}: {TotalCommittedBytes}");
                sb.AppendLine($"{nameof(PauseTimePercentage)}: {PauseTimePercentage}%");
                sb.AppendLine($"{nameof(PauseDuration)}: {PauseDuration}");

                return sb.ToString();
            }
        }
    }
}
