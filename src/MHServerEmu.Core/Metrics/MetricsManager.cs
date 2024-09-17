using System.Collections.Concurrent;
using System.Text;

namespace MHServerEmu.Core.Metrics
{
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
        
        public string GenerateReport()
        {
            StringBuilder sb = new();

            lock (_gamePerformanceMetricsDict)
            {
                foreach (GamePerformanceMetrics metrics in _gamePerformanceMetricsDict.Values)
                {
                    double min = metrics.MinFixedUpdateTime.TotalMilliseconds;
                    double max = metrics.MaxFixedUpdateTime.TotalMilliseconds;
                    double avg = metrics.CalculateAverageFixedUpdateTime().TotalMilliseconds;
                    double mdn = metrics.CalculateMedianFixedUpdateTime().TotalMilliseconds;

                    sb.AppendLine($"[0x{metrics.GameId:X}] min={min} ms, max={max} ms, avg={avg} ms, mdn={mdn} ms");
                }
            }

            return sb.ToString();
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
