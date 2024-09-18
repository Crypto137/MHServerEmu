using System.Text;
using System.Text.Json;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Core.Metrics
{
    public class PerformanceReport : IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Dictionary<ulong, GamePerformanceMetrics.Report> Games { get; } = new();

        public PerformanceReport() { }

        public void Initialize(IEnumerable<GamePerformanceMetrics> gameMetrics)
        {
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
            Games.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        private string AsPlainText()
        {
            StringBuilder sb = new();

            foreach (var kvp in Games)
                sb.AppendLine($"[0x{kvp.Key:X}] {kvp.Value}");

            return sb.ToString();
        }
    }
}
