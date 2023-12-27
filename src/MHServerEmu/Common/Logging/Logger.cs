namespace MHServerEmu.Common.Logging
{
    public class Logger
    {
        public enum Level
        {
            Trace,
            Debug,
            Info,
            Warn,
            Error,
            Fatal
        }

        public string Name { get; }

        public Logger(string name)
        {
            Name = name;
        }

        // Normal logging
        public void Trace(string message) => Log(Level.Trace, message);
        public void Debug(string message) => Log(Level.Debug, message);
        public void Info(string message) => Log(Level.Info, message);
        public void Warn(string message) => Log(Level.Warn, message);
        public void Error(string message) => Log(Level.Error, message);
        public void Fatal(string message) => Log(Level.Fatal, message);

        // Exception logging
        public void TraceException(Exception exception, string message) => LogException(Level.Trace, message, exception);
        public void DebugException(Exception exception, string message) => LogException(Level.Debug, message, exception);
        public void InfoException(Exception exception, string message) => LogException(Level.Info, message, exception);
        public void WarnException(Exception exception, string message) => LogException(Level.Warn, message, exception);
        public void ErrorException(Exception exception, string message) => LogException(Level.Error, message, exception);
        public void FatalException(Exception exception, string message) => LogException(Level.Fatal, message, exception);

        // Logging with a return value (for early returns)
        public T TraceReturn<T>(string message, T returnValue) => LogReturn(Level.Trace, message, returnValue);
        public T DebugReturn<T>(string message, T returnValue) => LogReturn(Level.Debug, message, returnValue);
        public T InfoReturn<T>(string message, T returnValue) => LogReturn(Level.Info, message, returnValue);
        public T WarnReturn<T>(string message, T returnValue) => LogReturn(Level.Warn, message, returnValue);
        public T ErrorReturn<T>(string message, T returnValue) => LogReturn(Level.Error, message, returnValue);
        public T FatalReturn<T>(string message, T returnValue) => LogReturn(Level.Fatal, message, returnValue);

        private void Log(Level level, string message) => LogRouter.EnqueueMessage(level, Name, message);
        private void LogException(Level level, string message, Exception exception) => Log(level, $"{message} - [Exception] {exception}");

        private T LogReturn<T>(Level level, string message, T returnValue)
        {
            Log(level, message);
            return returnValue;
        }
    }
}
