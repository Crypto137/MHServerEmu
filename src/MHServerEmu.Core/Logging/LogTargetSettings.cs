namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// Contains settings for a <see cref="LogTarget"/>.
    /// </summary>
    public class LogTargetSettings
    {
        public bool IncludeTimestamps { get; set; }
        public LoggingLevel MinimumLevel { get; set; }
        public LoggingLevel MaximumLevel { get; set; }
        public LogChannels Channels { get; set; }
    }
}
