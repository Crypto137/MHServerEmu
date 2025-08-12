using System.Diagnostics;

namespace MHServerEmu.Core.Metrics
{
    /// <summary>
    /// Starts a timer when it is created. Records the elapsed time as a <see cref="GamePerformanceMetricEnum"/> when it is disposed.
    /// </summary>
    public readonly struct GameProfileTimer : IDisposable
    {
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private readonly ulong _gameId;
        private readonly GamePerformanceMetricEnum _metric;
        private readonly TimeSpan _referenceTime;

        public GameProfileTimer(ulong gameId, GamePerformanceMetricEnum metric)
        {
            _gameId = gameId;
            _metric = metric;
            _referenceTime = Stopwatch.Elapsed;
        }

        public void Dispose()
        {
            TimeSpan stopTime = Stopwatch.Elapsed;
            TimeSpan elapsed = stopTime - _referenceTime;
            MetricsManager.Instance.RecordGamePerformanceMetric(_gameId, _metric, elapsed);
        }
    }
}
