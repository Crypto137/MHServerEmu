namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// An abstract class for logging output targets.
    /// </summary>
    public abstract class LogTarget
    {
        private readonly LogTargetSettings _settings;

        public bool IncludeTimestamps { get => _settings.IncludeTimestamps; }
        public LoggingLevel MinimumLevel { get => _settings.MinimumLevel; }
        public LoggingLevel MaximumLevel { get => _settings.MaximumLevel; }
        public LogChannels Channels { get => _settings.Channels; }

        /// <summary>
        /// Constructs a new <see cref="LogTarget"/> instance with the specified <see cref="LogTargetSettings"/>.
        /// </summary>
        public LogTarget(LogTargetSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Processes a <see cref="LogMessage"/>.
        /// </summary>
        public abstract void ProcessLogMessage(in LogMessage message);
    }
}
