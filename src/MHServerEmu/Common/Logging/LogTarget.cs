namespace MHServerEmu.Common.Logging
{
    public class LogTarget
    {
        public bool IncludeTimestamps { get; protected set; }
        public Logger.Level MinimumLevel { get; protected set; }
        public Logger.Level MaximumLevel { get; protected set; }

        public LogTarget(bool includeTimestamps, Logger.Level minimumLevel, Logger.Level maximumLevel)
        {
            IncludeTimestamps = includeTimestamps;
            MinimumLevel = minimumLevel;
            MaximumLevel = maximumLevel;
        }

        public virtual void LogMessage(LogMessage message) { throw new NotSupportedException(); }
    }
}
