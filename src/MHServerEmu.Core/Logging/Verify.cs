using System.Runtime.CompilerServices;

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
        /// Logs a message if the specified condition if the provided instance of <typeparamref name="T"/> is <see langword="null"/>.
        /// </summary>
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
        /// Logs a message with the specified <see cref="LoggingLevel"/> when a verify check fails.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void VerifyFail(string expression, string member, string file, int line, LoggingLevel level)
        {
            // This imitates Gazillion::Verify::VerifyFail(). We can potentially do other things with these here like Gazillion.
            Logger.Log(level, $"Verify failed: {expression}\n\tFile:{file} Line:{line} Member:{member}()");
        }
    }
}
