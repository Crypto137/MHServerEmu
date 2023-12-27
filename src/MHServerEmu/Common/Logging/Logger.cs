namespace MHServerEmu.Common.Logging
{
    /// <summary>
    /// Provides logging capabilities.
    /// </summary>
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

        private readonly string _name;

        /// <summary>
        /// Constructs a new instance of <see cref="Logger"/> with the specified name.
        /// </summary>
        public Logger(string name)
        {
            _name = name;
        }

        #region Normal Logging

        /// <summary>
        /// Logs a trace message.
        /// </summary>
        public void Trace(string message) => Log(Level.Trace, message);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public void Debug(string message) => Log(Level.Debug, message);

        /// <summary>
        /// Logs an info message.
        /// </summary>
        public void Info(string message) => Log(Level.Info, message);

        /// <summary>
        /// Logs a warn message.
        /// </summary>
        public void Warn(string message) => Log(Level.Warn, message);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public void Error(string message) => Log(Level.Error, message);

        /// <summary>
        /// Logs a fatal message.
        /// </summary>
        public void Fatal(string message) => Log(Level.Fatal, message);

        #endregion

        #region Exception Logging

        /// <summary>
        /// Logs an <see cref="Exception"/> as a trace message.
        /// </summary>
        public void TraceException(Exception exception, string message) => LogException(Level.Trace, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a debug message.
        /// </summary>
        public void DebugException(Exception exception, string message) => LogException(Level.Debug, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as an info message.
        /// </summary>
        public void InfoException(Exception exception, string message) => LogException(Level.Info, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a warn message.
        /// </summary>
        public void WarnException(Exception exception, string message) => LogException(Level.Warn, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as an error message.
        /// </summary>
        public void ErrorException(Exception exception, string message) => LogException(Level.Error, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a fatal message.
        /// </summary>
        public void FatalException(Exception exception, string message) => LogException(Level.Fatal, message, exception);

        #endregion

        #region Return Logging (for single line early returns)

        /// <summary>
        /// Logs a trace message and returns <typeparamref name="T"/>.
        /// </summary>
        public T TraceReturn<T>(T returnValue, string message) => LogReturn(Level.Trace, message, returnValue);

        /// <summary>
        /// Logs a debug message and returns <typeparamref name="T"/>.
        /// </summary>
        public T DebugReturn<T>(T returnValue, string message) => LogReturn(Level.Debug, message, returnValue);

        /// <summary>
        /// Logs an info message and returns <typeparamref name="T"/>.
        /// </summary>
        public T InfoReturn<T>(T returnValue, string message) => LogReturn(Level.Info, message, returnValue);

        /// <summary>
        /// Logs a warn message and returns <typeparamref name="T"/>.
        /// </summary>
        public T WarnReturn<T>(T returnValue, string message) => LogReturn(Level.Warn, message, returnValue);

        /// <summary>
        /// Logs an error message and returns <typeparamref name="T"/>.
        /// </summary>
        public T ErrorReturn<T>(T returnValue, string message) => LogReturn(Level.Error, message, returnValue);

        /// <summary>
        /// Logs a fatal message and returns <typeparamref name="T"/>.
        /// </summary>
        public T FatalReturn<T>(T returnValue, string message) => LogReturn(Level.Fatal, message, returnValue);

        #endregion

        /// <summary>
        /// Logs a message on the specified level.
        /// </summary>
        private void Log(Level level, string message) => LogRouter.EnqueueMessage(level, _name, message);

        /// <summary>
        /// Logs an exception on the specified level.
        /// </summary>
        private void LogException(Level level, string message, Exception exception) => Log(level, $"{message} - [Exception] {exception}");

        /// <summary>
        /// Logs a message on the specified level and returns <typeparamref name="T"/>.
        /// </summary>
        private T LogReturn<T>(Level level, string message, T returnValue)
        {
            Log(level, message);
            return returnValue;
        }
    }
}
