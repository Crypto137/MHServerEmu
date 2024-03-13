namespace MHServerEmu.Core.Logging
{
    public enum LoggingLevel
    {
        Trace,
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary>
    /// Provides logging capabilities.
    /// </summary>
    public class Logger
    {
        private readonly string _name;

        /// <summary>
        /// Constructs a new <see cref="Logger"/> instance with the specified name.
        /// </summary>
        public Logger(string name)
        {
            _name = name;
        }

        #region Normal Logging

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Trace"/> message.
        /// </summary>
        public void Trace(string message) => Log(LoggingLevel.Trace, message);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Debug"/> message.
        /// </summary>
        public void Debug(string message) => Log(LoggingLevel.Debug, message);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Info"/> message.
        /// </summary>
        public void Info(string message) => Log(LoggingLevel.Info, message);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Warn"/> message.
        /// </summary>
        public void Warn(string message) => Log(LoggingLevel.Warn, message);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Error"/> message.
        /// </summary>
        public void Error(string message) => Log(LoggingLevel.Error, message);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Fatal"/> message.
        /// </summary>
        public void Fatal(string message) => Log(LoggingLevel.Fatal, message);

        #endregion

        #region Exception Logging

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Trace"/> message.
        /// </summary>
        public void TraceException(Exception exception, string message) => LogException(LoggingLevel.Trace, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Debug"/> message.
        /// </summary>
        public void DebugException(Exception exception, string message) => LogException(LoggingLevel.Debug, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Info"/> message.
        /// </summary>
        public void InfoException(Exception exception, string message) => LogException(LoggingLevel.Info, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Warn"/> message.
        /// </summary>
        public void WarnException(Exception exception, string message) => LogException(LoggingLevel.Warn, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Error"/> message.
        /// </summary>
        public void ErrorException(Exception exception, string message) => LogException(LoggingLevel.Error, message, exception);

        /// <summary>
        /// Logs an <see cref="Exception"/> as a <see cref="LoggingLevel.Fatal"/> message.
        /// </summary>
        public void FatalException(Exception exception, string message) => LogException(LoggingLevel.Fatal, message, exception);

        #endregion

        #region Return Logging (for single line early returns)

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Trace"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T TraceReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Trace, message, returnValue);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Debug"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T DebugReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Debug, message, returnValue);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Info"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T InfoReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Info, message, returnValue);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Warn"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T WarnReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Warn, message, returnValue);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Error"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T ErrorReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Error, message, returnValue);

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Fatal"/> message and returns <typeparamref name="T"/>.
        /// </summary>
        public T FatalReturn<T>(T returnValue, string message) => LogReturn(LoggingLevel.Fatal, message, returnValue);

        #endregion

        /// <summary>
        /// Logs a message on the specified <see cref="LoggingLevel"/>.
        /// </summary>
        private void Log(LoggingLevel level, string message) => LogRouter.AddMessage(level, _name, message);

        /// <summary>
        /// Logs an exception on the specified level.
        /// </summary>
        private void LogException(LoggingLevel level, string message, Exception exception) => Log(level, $"{message} - [Exception] {exception}");

        /// <summary>
        /// Logs a message on the specified <see cref="LoggingLevel"/> and returns <typeparamref name="T"/>.
        /// </summary>
        private T LogReturn<T>(LoggingLevel level, string message, T returnValue)
        {
            Log(level, message);
            return returnValue;
        }

        public static string ObjectCollectionToString(IEnumerable<object> collection)
        {
            string output = "{";
            foreach (var item in collection)
                output += $"{item} ";
            output += "}";

            return output;
        }
    }
}
