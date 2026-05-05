using System.Runtime.CompilerServices;
using System.Text;

namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// Checks conditions and logs messages when they are not met.
    /// </summary>
    public static class Verify
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // This mimics the Assert API used in things like xunit.

        /// <summary>
        /// Logs a message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerArgumentExpression(nameof(condition))] string expression = null,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (condition == false)
                VerifyFail(expression, member, file, line, loggingLevel);

            return condition;
        }

        /// <summary>
        /// Logs a message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            string message,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (condition == false)
                VerifyFail(message, member, file, line, loggingLevel);

            return condition;
        }

        /// <summary>
        /// Logs a message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            [InterpolatedStringHandlerArgument(nameof(condition))] ref InterpolatedStringHandler message,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (condition == false)
                VerifyFail(message.ToString(), member, file, line, loggingLevel);

            return condition;
        }

        /// <summary>
        /// Logs a message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerArgumentExpression(nameof(instance))] string expression = null,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0) where T: class
        {
            if (instance == null)
            {
                VerifyFail(expression, member, file, line, loggingLevel);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs a message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            string message,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0) where T : class
        {
            if (instance == null)
            {
                VerifyFail(message, member, file, line, loggingLevel);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs a message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            [InterpolatedStringHandlerArgument(nameof(instance))] ref InterpolatedStringHandler message,
            LoggingLevel loggingLevel = LoggingLevel.Warn,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0) where T : class
        {
            if (instance == null)
            {
                VerifyFail(message.ToString(), member, file, line, loggingLevel);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs a message with the specified <see cref="LoggingLevel"/> when a verify check fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void VerifyFail(string message, string member, string file, int line, LoggingLevel level)
        {
            // This imitates Gazillion::Verify::VerifyFail(). We can potentially do other things with these here like Gazillion.
            Logger.Log(level, $"Verify failed: {message}\n\tFile:{file} Line:{line} Member:{member}()");
        }

        [InterpolatedStringHandler]
        public readonly ref struct InterpolatedStringHandler
        {
            private readonly StringBuilder _sb;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public InterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool isEnabled)
            {
                if (condition == false)
                    _sb = new();

                isEnabled = _sb != null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public InterpolatedStringHandler(int literalLength, int formattedCount, object instance, out bool isEnabled)
            {
                if (instance == null)
                    _sb = new();

                isEnabled = _sb != null;
            }

            public override string ToString()
            {
                return _sb?.ToString();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendLiteral(string s)
            {
                _sb.Append(s);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendFormatted<T>(T t)
            {
                _sb.Append(t?.ToString());
            }
        }
    }
}
