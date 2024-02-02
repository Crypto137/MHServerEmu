using System.Diagnostics;

namespace MHServerEmu.Common.Logging
{
    /// <summary>
    /// Manages <see cref="Logger"/> and <see cref="LogTarget"/> instances.
    /// </summary>
    public static class LogManager
    {
        private static readonly Dictionary<string, Logger> _loggerDict = new();
        private static readonly HashSet<LogTarget> _targets = new();

        public static bool Enabled { get; set; }

        /// <summary>
        /// Creates or returns existing <see cref="Logger"/> instance with the same name as the caller's <see cref="Type"/>.
        /// </summary>
        public static Logger CreateLogger()
        {
            StackFrame stackFrame = new(1, false);
            string callerName = stackFrame.GetMethod().DeclaringType.Name;
            return callerName == null
                ? throw new Exception("LogManager failed to get caller name when creating a logger.")
                : CreateLogger(callerName);
        }

        /// <summary>
        /// Creates or returns existing <see cref="Logger"/> instance with the specified name.
        /// </summary>
        public static Logger CreateLogger(string name)
        {
            if (_loggerDict.TryGetValue(name, out var logger) == false)
            {
                logger = new(name);
                _loggerDict.Add(name, logger);
            }

            return logger;
        }

        /// <summary>
        /// Attaches a new <see cref="LogTarget"/> to route <see cref="LogMessage"/> instances to. Returns <see langword="true"/> if successful.
        /// </summary>
        public static bool AttachTarget(LogTarget target)
        {
            return _targets.Add(target);
        }

        /// <summary>
        /// Iterates through all attached <see cref="LogTarget"/> instances that accept the specified <see cref="LoggingLevel"/>.
        /// </summary>
        internal static IEnumerable<LogTarget> IterateTargets(LoggingLevel level)
        {
            return _targets.Where(target => (level >= target.MinimumLevel) && (level <= target.MaximumLevel));
        }
    }
}
