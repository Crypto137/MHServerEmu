using System.Diagnostics;

namespace MHServerEmu.Common
{
    public static class LogManager
    {
        private static readonly Dictionary<string, Logger> LoggerDict = new();

        public static Logger CreateLogger()
        {
            StackFrame stackFrame = new(1, false);
            string callerName = stackFrame.GetMethod().DeclaringType.Name;
            if (callerName == null) throw new Exception("LogManager: failed to get caller name when creating a logger");
            if (LoggerDict.ContainsKey(callerName) == false) LoggerDict.Add(callerName, new Logger(callerName));
            return LoggerDict[callerName];
        }

        public static Logger CreateLogger(string name)
        {
            if (LoggerDict.ContainsKey(name) == false) LoggerDict.Add(name, new Logger(name));
            return LoggerDict[name];
        }

    }

    public class Logger
    {
        private static bool _enableTimestamps = true;

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
