using System.Collections.Concurrent;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics.Categories;

namespace MHServerEmu.Core.Metrics
{
    public class MetricsManager
    {
        private const int UpdateTickIntervalMS = 1000;
        private const int MemoryUpdateIntervalTicks = 3;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _lock = new();

        private readonly MemoryMetrics _memoryMetrics = new();

        private readonly ConcurrentQueue<(ulong, GamePerformanceMetricValue)> _gamePerformanceMetricQueue = new();
        private readonly ConcurrentQueue<ulong> _gameInstancesToRemove = new();
        private readonly Dictionary<ulong, GamePerformanceMetrics> _gamePerformanceMetricsDict = new();

        private long _tick;

        public static MetricsManager Instance { get; } = new();

        private MetricsManager()
        {
            Logger.Info("Started processing metrics");
            Task.Run(UpdateAsync);
        }

        public void Update()
        {
            lock (_lock)
            {
                _tick++;

                // Sample new GC data
                if (_tick % MemoryUpdateIntervalTicks == 0)
                    _memoryMetrics.Update();

                // Update game performance metrics
                while (_gamePerformanceMetricQueue.TryDequeue(out var entry))
                {
                    ulong gameId = entry.Item1;
                    GamePerformanceMetricValue metricValue = entry.Item2;

                    if (_gamePerformanceMetricsDict.TryGetValue(gameId, out GamePerformanceMetrics gameMetrics) == false)
                    {
                        gameMetrics = new(gameId);
                        _gamePerformanceMetricsDict.TryAdd(gameId, gameMetrics);
                    }

                    gameMetrics.Update(metricValue);
                }

                // Remove game instances that were shut down
                while (_gameInstancesToRemove.TryDequeue(out ulong gameId))
                    _gamePerformanceMetricsDict.Remove(gameId);
            }
        }

        public void RecordGamePerformanceMetric(ulong gameId, GamePerformanceMetricEnum metric, float value)
        {
            _gamePerformanceMetricQueue.Enqueue((gameId, new(metric, value)));
        }

        public void RecordGamePerformanceMetric(ulong gameId, GamePerformanceMetricEnum metric, TimeSpan value)
        {
            _gamePerformanceMetricQueue.Enqueue((gameId, new(metric, value)));
        }

        public void RemoveGameInstance(ulong gameId)
        {
            _gameInstancesToRemove.Enqueue(gameId);
        }

        public string GeneratePerformanceReport(MetricsReportFormat format)
        {
            using PerformanceReport report = ObjectPoolManager.Instance.Get<PerformanceReport>();

            lock (_lock)
                report.Initialize(_memoryMetrics, _gamePerformanceMetricsDict.Values);

            return report.ToString(format);
        }

        private async void UpdateAsync()
        {
            while (true)
            {
                Update();
                await Task.Delay(UpdateTickIntervalMS);
            }
        }
    }
}
