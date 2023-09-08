namespace MHServerEmu.Common.Logging
{
    internal static class LogRouter
    {
        public static void RouteMessage(Logger.Level level, string logger, string message)
        {
            if (LogManager.Enabled == false || LogManager.TargetList.Count == 0) return;

            foreach (LogTarget target in LogManager.TargetList.Where(target => (level >= target.MinimumLevel) && (level <= target.MaximumLevel)))
                target.LogMessage(level, logger, message);
        }

        public static void RouteException(Logger.Level level, string logger, string message, Exception exception)
        {
            if (LogManager.Enabled == false || LogManager.TargetList.Count == 0) return;

            foreach (LogTarget target in LogManager.TargetList.Where(target => (level >= target.MinimumLevel) && (level <= target.MaximumLevel)))
                target.LogException(level, logger, message, exception);
        }
    }
}
