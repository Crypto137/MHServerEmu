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
            Span<char> formattedTimestamp = stackalloc char[TimeFormat.Length];
            Timestamp.TryFormat(formattedTimestamp, out _, TimeFormat);
            return $"[{formattedTimestamp}] [{Level,5}] [{Logger}] {Message}";
        }

        /// <summary>
        /// Returns a string that represents this <see cref="LogMessage"/> with or without a timestamp.
        /// </summary>
        public string ToString(bool includeTimestamp)
        {
            if (includeTimestamp)
                return ToString();

            return $"[{Level,5}] [{Logger}] {Message}";
        }

        public void WriteTo(TextWriter writer, bool includeTimestamp, bool writeLine)
        {
            // TextWriter.Write() with format arguments causes less memory allocation than string interpolation as of .NET 8.
            // We can't use this for our timestamp though because Span cannot be cast to an object.

            // We can potentially make use of interpolated strings here without sacrificing performance with a custom InterpolatedStringHandler implementation.
            // https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/interpolated-string-handler

            if (includeTimestamp)
            {
                Span<char> formattedTimestamp = stackalloc char[TimeFormat.Length];
                Timestamp.TryFormat(formattedTimestamp, out _, TimeFormat);
                writer.Write('[');
                writer.Write(formattedTimestamp);
                writer.Write(']');
                writer.Write(' ');
            }

            writer.Write("[{0,5}] [{1}] {2}", Level.ToString(), Logger, Message);

            if (writeLine)
                writer.WriteLine();
        }
    }
}
