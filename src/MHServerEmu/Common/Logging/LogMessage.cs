using System.Text;

namespace MHServerEmu.Common.Logging
{
    public class LogMessage
    {
        public DateTime Timestamp { get; }
        public Logger.Level Level { get; }
        public string Logger { get; }
        public string Message { get; }

        public LogMessage(Logger.Level level, string logger, string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Logger = logger;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy.MM.dd HH:mm:ss.fff}] [{Level,5}] [{Logger}] {Message}";
        }

        public string ToString(bool includeTimestamps)
        {
            StringBuilder sb = new();
            if (includeTimestamps) sb.Append($"[{Timestamp:yyyy.MM.dd HH:mm:ss.fff}] ");
            sb.Append($"[{Level,5}] [{Logger}] {Message}");
            return sb.ToString();
        }
    }
}
