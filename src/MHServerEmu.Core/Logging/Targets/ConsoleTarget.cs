namespace MHServerEmu.Core.Logging.Targets
{
    /// <summary>
    /// Outputs <see cref="LogMessage"/> instances to the <see cref="Console"/>.
    /// </summary>
    public class ConsoleTarget : LogTarget
    {
        /// <summary>
        /// Constructs a new <see cref="ConsoleTarget"/> instance with the specified parameters.
        /// </summary>
        public ConsoleTarget(LogTargetSettings settings) : base(settings)
        {
        }

        /// <summary>
        /// Outputs a <see cref="LogMessage"/> instance to the <see cref="Console"/>.
        /// </summary>
        public override void ProcessLogMessage(in LogMessage message)
        {
            SetForegroundColor(message.Level);
            Console.WriteLine(message.ToString(IncludeTimestamps));
            Console.ResetColor();
        }

        /// <summary>
        /// Sets <see cref="Console.ForegroundColor"/> to the appropriate value for the specified <see cref="LoggingLevel"/>.
        /// </summary>
        private static void SetForegroundColor(LoggingLevel level)
        {
            switch (level)
            {
                case LoggingLevel.Trace:    Console.ForegroundColor = ConsoleColor.DarkGray;    break;
                case LoggingLevel.Debug:    Console.ForegroundColor = ConsoleColor.Cyan;        break;
                case LoggingLevel.Info:     Console.ForegroundColor = ConsoleColor.White;       break;
                case LoggingLevel.Warn:     Console.ForegroundColor = ConsoleColor.Yellow;      break;
                case LoggingLevel.Error:    Console.ForegroundColor = ConsoleColor.Magenta;     break;
                case LoggingLevel.Fatal:    Console.ForegroundColor = ConsoleColor.Red;         break;
            }
        }
    }
}
