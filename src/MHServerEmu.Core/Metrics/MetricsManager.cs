using System.Collections.Concurrent;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Core.Metrics
{
    public enum MetricsReportFormat
    {
        PlainText,
        Json
    }

    public class MetricsManager
    {
        private const int UpdateIntervalMS = 1000;

        private readonly ConcurrentQueue<(ulong, TimeSpan)> _fixedUpdateTimeQueue = new();
        private readonly Dictionary<ulong, GamePerformanceMetrics> _gamePerformanceMetricsDict = new();

        public static MetricsManager Instance { get; } = new();

        private MetricsManager()
        {
            Task.Run(UpdateAsync);
        }

        public void Update()
        {
            while (_fixedUpdateTimeQueue.TryDequeue(out var entry))
            {
                ulong gameId = entry.Item1;
                TimeSpan fixedUpdateTime = entry.Item2;

                lock (_gamePerformanceMetricsDict)
                {
                    if (_gamePerformanceMetricsDict.TryGetValue(gameId, out GamePerformanceMetrics metrics) == false)
                    {
                        metrics = new(gameId);
                        _gamePerformanceMetricsDict.TryAdd(gameId, metrics);
                    }

                    metrics.RecordFixedUpdateTime(fixedUpdateTime);
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

            lock (_gamePerformanceMetricsDict)
                report.Initialize(_gamePerformanceMetricsDict.Values);

            return report.ToString(format);
        }

        private async void UpdateAsync()
        {
            while (true)
            {
                Update();
                await Task.Delay(UpdateIntervalMS);
            }
        }
    }
}
