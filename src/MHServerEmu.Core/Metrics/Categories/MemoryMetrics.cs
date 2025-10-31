using System.Runtime;
using System.Text;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Metrics.Categories
{
    public class MemoryMetrics
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private long _gcIndex = 0;
        private long[] _gcKindIndices = new long[4];
        private long[] _gcCounts = new long[GC.MaxGeneration + 1];
        private long _totalCommittedBytes;
        private long _heapSizeBytes;
        private double _pauseTimePercentage;
        private readonly MetricTracker _pauseDurationTracker = new("PauseDuration", 512);

        public MemoryMetrics()
        {
            Logger.Trace($"Garbage collection mode: {(GCSettings.IsServerGC ? "Server" : "Workstation")}");
        }

        public void Update()
        {
            // Update data for each GCKind separately
            UpdateForGCKind(GCKind.Ephemeral);
            UpdateForGCKind(GCKind.FullBlocking);
            UpdateForGCKind(GCKind.Background);
        }

        public Report GetReport()
        {
            return new(this);
        }

        private void UpdateForGCKind(GCKind gcKind)
        {
            GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo(gcKind);

            // Check if we are up to date with this kind
            if (memoryInfo.Index <= _gcKindIndices[(int)gcKind])
                return;

            // Update metrics tracked for all collections
            _gcKindIndices[(int)gcKind] = memoryInfo.Index;
            _gcCounts[memoryInfo.Generation]++;

            TimeSpan combinedPauseDuration = memoryInfo.PauseDurations[0] + memoryInfo.PauseDurations[1];
            _pauseDurationTracker.Track(combinedPauseDuration);

            if (gcKind != GCKind.Ephemeral)
                Logger.Trace($"{gcKind} GC recorded: Index={memoryInfo.Index}, Generation={memoryInfo.Generation}, PauseDuration={combinedPauseDuration.TotalMilliseconds} ms");

            // Update metrics tracked for the most recent collection
            if (memoryInfo.Index > _gcIndex)
            {
                _gcIndex = memoryInfo.Index;
                _totalCommittedBytes = memoryInfo.TotalCommittedBytes;
                _heapSizeBytes = memoryInfo.HeapSizeBytes;
                _pauseTimePercentage = memoryInfo.PauseTimePercentage;
            }
        }

        public readonly struct Report
        {
            public long GCIndex { get; }
            public long GCCountGen0 { get; }
            public long GCCountGen1 { get; }
            public long GCCountGen2 { get; }
            public long TotalCommittedBytes { get; }
            public long HeapSizeBytes { get; }
            public double PauseTimePercentage { get; }
            public MetricTracker.ReportEntry PauseDuration { get; }

            public Report(MemoryMetrics metrics)
            {
                GCIndex = metrics._gcIndex;
                GCCountGen0 = metrics._gcCounts[0];
                GCCountGen1 = metrics._gcCounts[1];
                GCCountGen2 = metrics._gcCounts[2];
                TotalCommittedBytes = metrics._totalCommittedBytes;
                HeapSizeBytes = metrics._heapSizeBytes;
                PauseTimePercentage = metrics._pauseTimePercentage;
                PauseDuration = metrics._pauseDurationTracker.AsReportEntry();
            }

            public override string ToString()
            {
                StringBuilder sb = new();

                sb.AppendLine($"{nameof(GCIndex)}: {GCIndex}");
                sb.AppendLine($"GCCounts: Gen0={GCCountGen0}, Gen1={GCCountGen1}, Gen2={GCCountGen2}");
                sb.AppendLine($"{nameof(HeapSizeBytes)}: {HeapSizeBytes:N0} / {TotalCommittedBytes:N0}");
                sb.AppendLine($"{nameof(PauseTimePercentage)}: {PauseTimePercentage}%");
                sb.AppendLine($"{nameof(PauseDuration)}: {PauseDuration}");

                return sb.ToString();
            }
        }
    }
}
