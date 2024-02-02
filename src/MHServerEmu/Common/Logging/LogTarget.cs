namespace MHServerEmu.Common.Logging
{
    /// <summary>
    /// An abstract class for logging output targets.
    /// </summary>
    public abstract class LogTarget
    {
        public bool IncludeTimestamps { get; protected set; }
        public LoggingLevel MinimumLevel { get; protected set; }
        public LoggingLevel MaximumLevel { get; protected set; }

        /// <summary>
        /// Constructs a new <see cref="LogTarget"/> instance with the specified parameters.
        /// </summary>
        public LogTarget(bool includeTimestamps, LoggingLevel minimumLevel, LoggingLevel maximumLevel)
        {
            IncludeTimestamps = includeTimestamps;
            MinimumLevel = minimumLevel;
            MaximumLevel = maximumLevel;
        }

        /// <summary>
        /// Processes a <see cref="LogMessage"/>.
        /// </summary>
        public abstract void ProcessLogMessage(LogMessage message);
    }
}
