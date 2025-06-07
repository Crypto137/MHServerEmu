using System.Text;
using System.Text.Json;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics.Categories;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Metrics
{
    public class PerformanceReport : IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static uint _currentReportId = 0;

        public ulong Id { get; private set; }
        public MemoryMetrics.Report Memory { get; private set; }
        public Dictionary<ulong, GamePerformanceMetrics.Report> Games { get; } = new();

        public bool IsInPool { get; set; }

        public PerformanceReport() { }

        public void Initialize(MemoryMetrics memoryMetrics, IEnumerable<GamePerformanceMetrics> gameMetrics)
        {
            Id = (ulong)Clock.UnixTime.TotalSeconds << 32 | ++_currentReportId;

            Memory = memoryMetrics.GetReport();

            foreach (GamePerformanceMetrics metrics in gameMetrics)
            {
                Games.Add(metrics.GameId, metrics.GetReport());
            }
        }

        public override string ToString()
        {
            return ToString(MetricsReportFormat.PlainText);
        }

        public string ToString(MetricsReportFormat format)
        {
            switch (format)
            {
                case MetricsReportFormat.PlainText:
                    return AsPlainText();

                case MetricsReportFormat.Json:
                    return JsonSerializer.Serialize(this);

                default:
                    return Logger.WarnReturn(string.Empty, $"ToString(): Unsupported format {format}");
            }
        }

        public void ResetForPool()
        {
            Memory = default;
            Games.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        private string AsPlainText()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Performance Report 0x{Id:X}");

            sb.AppendLine("Memory:");
            sb.AppendLine(Memory.ToString());

            sb.AppendLine("Games:");
            foreach (var kvp in Games)
            {
                sb.AppendLine($"Game [0x{kvp.Key:X}]:");
                sb.AppendLine(kvp.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
