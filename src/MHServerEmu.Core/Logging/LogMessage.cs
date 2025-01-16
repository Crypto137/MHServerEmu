using System.Text;

namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// A timestamped log message.
    /// </summary>
    public readonly struct LogMessage
    {
        private const string TimeFormat = "yyyy.MM.dd HH:mm:ss.fff";

        private static readonly StringBuilder StringBuilder = new();

        public DateTime Timestamp { get; }
        public LoggingLevel Level { get; }
        public string Logger { get; }
        public string Message { get; }

        // Additional metadata for filtering
        public LogChannels Channels { get; }
        public LogCategory Category { get; }

        /// <summary>
        /// Constructs a new <see cref="LogMessage"/> instance with the specified parameters.
        /// </summary>
        public LogMessage(LoggingLevel level, string logger, string message, LogChannels channels, LogCategory category)
        {
            Timestamp = LogManager.LogTimeNow;
            Level = level;
            Logger = logger;
            Message = message;

            Channels = channels;
            Category = category;
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
            lock (StringBuilder)    // This shouldn't be called from multiple threads unless in synchronous mode
            {
                if (includeTimestamps)
                    StringBuilder.Append($"[{Timestamp.ToString(TimeFormat)}] ");

                StringBuilder.Append($"[{Level,5}] [{Logger}] {Message}");

                string str = StringBuilder.ToString();
                StringBuilder.Clear();

                return str;
            }
        }
    }
}
