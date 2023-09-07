using MHServerEmu.Common.Config;

namespace MHServerEmu.Common.Logging
{
    public class Logger
    {
        private static bool _enableTimestamps = ConfigManager.Server.EnableTimestamps;

        public enum Level
        {
            Trace,
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }

        public string Name { get; protected set; }

        public Logger(string name)
        {
            Name = name;
        }

        public void Trace(string message) { Log(Level.Trace, message); }
        public void Debug(string message) { Log(Level.Debug, message); }
        public void Info(string message) { Log(Level.Info, message); }
        public void Warn(string message) { Log(Level.Warn, message); }
        public void Error(string message) { Log(Level.Error, message); }
        public void Fatal(string message) { Log(Level.Fatal, message); }

        private void Log(Level level, string message)
        {
            SetForegroundColor(level);
            string timestamp = _enableTimestamps ? $"[{DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff")}] " : "";
            Console.WriteLine($"{timestamp}[{level.ToString().PadLeft(5)}] [{Name}] {message}");
            Console.ResetColor();
        }

        private static void SetForegroundColor(Level level)
        {
            switch (level)
            {
                case Level.Trace: Console.ForegroundColor = ConsoleColor.DarkGray; break;
                case Level.Debug: Console.ForegroundColor = ConsoleColor.Cyan; break;
                case Level.Info: Console.ForegroundColor = ConsoleColor.White; break;
                case Level.Warn: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case Level.Error: Console.ForegroundColor = ConsoleColor.Magenta; break;
                case Level.Fatal: Console.ForegroundColor = ConsoleColor.Red; break;
                default: break;
            }
        }

        private static void ResetForegroundColor()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
