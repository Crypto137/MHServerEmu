namespace MHServerEmu.Common.Logging
{
    public class LogTarget
    {
        public bool IncludeTimestamps { get; protected set; }
        public Logger.Level MinimumLevel { get; protected set; }
        public Logger.Level MaximumLevel { get; protected set; }

        public virtual void LogMessage(Logger.Level level, string logger, string message) { throw new NotSupportedException(); }
        public virtual void LogException(Logger.Level level, string logger, string message, Exception exception) { throw new NotSupportedException(); }
    }
}
