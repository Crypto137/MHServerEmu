using MHServerEmu.Core.Metrics.Entries;
using MHServerEmu.Core.Metrics.Trackers;

namespace MHServerEmu.Core.Metrics.Categories
{
    public class GamePerformanceMetrics
    {
        private readonly object _lock = new();

        private readonly TimeTracker _fixedUpdateTimeTracker = new(1024);    // At 20 FPS this gives us about 51.2 seconds of data

        public ulong GameId { get; }

        public GamePerformanceMetrics(ulong gameId)
        {
            GameId = gameId;
        }

        public void Update(TimeSpan fixedUpdateTime)
        {
            _fixedUpdateTimeTracker.Track(fixedUpdateTime);
            // add more data here
        }

        public Report GetReport()
        {
            return new(this);
        }

        public readonly struct Report
        {
            public ReportTimeEntry FixedUpdateTime { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                FixedUpdateTime = metrics._fixedUpdateTimeTracker.AsReportEntry();
            }

            public override string ToString()
            {
                return $"{nameof(FixedUpdateTime)}: {FixedUpdateTime}";
            }
        }
    }
}
