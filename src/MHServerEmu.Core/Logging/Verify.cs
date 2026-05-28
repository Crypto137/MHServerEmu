using System.Runtime.CompilerServices;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Core.Logging
{
    /// <summary>
    /// Checks conditions and logs messages when they are not met.
    /// </summary>
    public static class Verify
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly Dictionary<(string File, int Line), int> KnownFailures = new();

        // This mimics the Assert API used in things like xunit.

        #region IsTrue

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
        /// Logs a <see cref="LoggingLevel.Warn"/> message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            string message,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            return IsTrue(condition, LoggingLevel.Warn, message, member, file, line);
        }

        /// <summary>
        /// Logs a message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            LoggingLevel loggingLevel,
            [InterpolatedStringHandlerArgument(nameof(condition))] ref VerifyInterpolatedStringHandler message,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            if (condition == false)
                VerifyFail(message.ToString(), member, file, line, loggingLevel);

            return condition;
        }

        /// <summary>
        /// Logs a <see cref="LoggingLevel.Warn"/> message if the specified condition is not <see langword="true"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTrue(bool condition,
            [InterpolatedStringHandlerArgument(nameof(condition))] ref VerifyInterpolatedStringHandler message,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0)
        {
            return IsTrue(condition, LoggingLevel.Warn, message.ToString(), member, file, line);
        }

        #endregion

        #region IsNotNull

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
        /// Logs a <see cref="LoggingLevel.Warn"/> message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            string message,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0) where T : class
        {
            return IsNotNull(instance, LoggingLevel.Warn, message, member, file, line);
        }

        /// <summary>
        /// Logs a message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            LoggingLevel loggingLevel,
            [InterpolatedStringHandlerArgument(nameof(instance))] ref VerifyInterpolatedStringHandler message,
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
        /// Logs a <see cref="LoggingLevel.Warn"/> message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNotNull<T>(T instance,
            [InterpolatedStringHandlerArgument(nameof(instance))] ref VerifyInterpolatedStringHandler message,
            [CallerMemberName] string member = null,
            [CallerFilePath] string file = null,
            [CallerLineNumber] int line = 0) where T : class
        {
            return IsNotNull(instance, LoggingLevel.Warn, ref message, member, file, line);
        }

        #endregion

        /// <summary>
        /// Adds counts for verify failures encountered since the last server restart to the provided <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        public static void GetKnownFailures(Dictionary<(string File, int Line), int> outKnownFailures)
        {
            lock (KnownFailures)
            {
                foreach (var kvp in KnownFailures)
                    outKnownFailures.Add(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Logs a message with the specified <see cref="LoggingLevel"/> when a verify check fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void VerifyFail(string message, string member, string file, int line, LoggingLevel level)
        {
            // This imitates Gazillion::Verify::VerifyFail(). We can potentially do other things with these here like Gazillion.

            // Include a stack trace when a verify is encountered for the first time on a particular line.
            string stackTrace = string.Empty;
            bool isNew;

            lock (KnownFailures)
                KnownFailures.GetValueRefOrAddDefault((file, line), out isNew)++;

            if (isNew)
                stackTrace = $" StackTrace:\n{Environment.StackTrace}";

            Logger.Log(level, $"Verify failed: {message}\n\tFile:{file} Line:{line} Member:{member}(){stackTrace}", LogChannels.General, LogCategory.Verify);
        }
    }
}
