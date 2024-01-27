namespace MHServerEmu.Common.Logging.Targets
{
    public class ConsoleTarget : LogTarget
    {
        public ConsoleTarget(bool includeTimestamps, Logger.Level minimumLevel, Logger.Level maximumLevel) : base(includeTimestamps, minimumLevel, maximumLevel) { }

        public override void LogMessage(LogMessage message)
        {
            SetForegroundColor(message.Level);
            Console.WriteLine(message.ToString(IncludeTimestamps));
            Console.ResetColor();
        }

        private static void SetForegroundColor(Logger.Level level)
        {
            switch (level)
            {
                case Logger.Level.Trace:    Console.ForegroundColor = ConsoleColor.DarkGray;    break;
                case Logger.Level.Debug:    Console.ForegroundColor = ConsoleColor.Cyan;        break;
                case Logger.Level.Info:     Console.ForegroundColor = ConsoleColor.White;       break;
                case Logger.Level.Warn:     Console.ForegroundColor = ConsoleColor.Yellow;      break;
                case Logger.Level.Error:    Console.ForegroundColor = ConsoleColor.Magenta;     break;
                case Logger.Level.Fatal:    Console.ForegroundColor = ConsoleColor.Red;         break;
            }
        }
    }
}
