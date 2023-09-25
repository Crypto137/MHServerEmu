namespace MHServerEmu.Common.Logging.Targets
{
    public class ConsoleTarget : LogTarget
    {
        private readonly object _writeLock = new();

        public ConsoleTarget(bool includeTimestamps, Logger.Level minimumLevel, Logger.Level maximumLevel) : base(includeTimestamps, minimumLevel, maximumLevel) { }

        public override void LogMessage(Logger.Level level, string logger, string message)
        {
            lock (_writeLock)
            {
                SetForegroundColor(level);
                string timestamp = IncludeTimestamps ? $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss.fff}] " : "";
                Console.WriteLine($"{timestamp}[{level,5}] [{logger}] {message}");
                Console.ResetColor();
            }
        }

        public override void LogException(Logger.Level level, string logger, string message, Exception exception)
        {
            lock (_writeLock)
            {
                SetForegroundColor(level);
                string timestamp = IncludeTimestamps ? $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss.fff}] " : "";
                Console.WriteLine($"{timestamp}[{level,5}] [{logger}] {message} - [Exception] {exception}");
                Console.ResetColor();
            }
        }

        private static void SetForegroundColor(Logger.Level level)
        {
            switch (level)
            {
                case Logger.Level.Trace: Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case Logger.Level.Debug: Console.ForegroundColor = ConsoleColor.Cyan; break;
                case Logger.Level.Info: Console.ForegroundColor = ConsoleColor.White; break;
                case Logger.Level.Warn: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case Logger.Level.Error: Console.ForegroundColor = ConsoleColor.Magenta; break;
                case Logger.Level.Fatal: Console.ForegroundColor = ConsoleColor.Red; break;
                default: break;
            }
        }
    }
}
