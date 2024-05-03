using System.Text;

namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// A timestamped log message.
    /// </summary>
    public readonly struct LogMessage
    {
        private const string TimeFormat = "yyyy.MM.dd HH:mm:ss.fff";

        public DateTime Timestamp { get; }
        public LoggingLevel Level { get; }
        public string Logger { get; }
        public string Message { get; }

        /// <summary>
        /// Constructs a new <see cref="LogMessage"/> instance with the specified parameters.
        /// </summary>
        public LogMessage(LoggingLevel level, string logger, string message)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Logger = logger;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Timestamp.ToString(TimeFormat)}] [{Level,5}] [{Logger}] {Message}";
        }

        /// <summary>
        /// Returns a string that represents this <see cref="LogMessage"/> with or without a timestamp.
        /// </summary>
        public string ToString(bool includeTimestamps)
        {
            StringBuilder sb = new();
            if (includeTimestamps) sb.Append($"[{Timestamp.ToString(TimeFormat)}] ");
            sb.Append($"[{Level,5}] [{Logger}] {Message}");
            return sb.ToString();
        }
    }
}
