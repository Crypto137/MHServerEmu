using System.Collections.Concurrent;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics.Categories;

namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json
    }

    public class MetricsManager
    {
        private const int UpdateTickIntervalMS = 1000;

        private readonly object _lock = new();

        private readonly MemoryMetrics _memoryMetrics = new();

        private readonly ConcurrentQueue<(ulong, TimeSpan)> _fixedUpdateTimeQueue = new();
        private readonly Dictionary<ulong, GamePerformanceMetrics> _gamePerformanceMetricsDict = new();

        private long _tick;

        public static MetricsManager Instance { get; } = new();

        private MetricsManager()
        {
            Task.Run(UpdateAsync);
        }

        public void Update()
        {
            lock (_lock)
            {
                _tick++;

                // Sample new GC data
                _memoryMetrics.Update();

                // Update game performance metrics
                while (_fixedUpdateTimeQueue.TryDequeue(out var entry))
                {
                    ulong gameId = entry.Item1;
                    TimeSpan fixedUpdateTime = entry.Item2;

                    if (_gamePerformanceMetricsDict.TryGetValue(gameId, out GamePerformanceMetrics gameMetrics) == false)
                    {
                        gameMetrics = new(gameId);
                        _gamePerformanceMetricsDict.TryAdd(gameId, gameMetrics);
                    }

                    gameMetrics.Update(fixedUpdateTime);
                }
            }
        }

        public void RecordFixedUpdateTime(ulong gameId, TimeSpan fixedUpdateTime)
        {
            _fixedUpdateTimeQueue.Enqueue((gameId, fixedUpdateTime));
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
