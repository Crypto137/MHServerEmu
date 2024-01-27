using System.Diagnostics;

namespace MHServerEmu.Common.Logging
{
    public static class LogManager
    {
        public static bool Enabled { get; set; }

        public static readonly List<LogTarget> TargetList = new();
        internal static readonly Dictionary<string, Logger> LoggerDict = new();

        public static void AttachLogTarget(LogTarget target) => TargetList.Add(target);

        /// <summary>
        /// Creates a new logger named based on the caller's name from StackFrame.
        /// </summary>
        public static Logger CreateLogger()
        {
            // Try to get caller name from the StackFrame
            StackFrame stackFrame = new(1, false);
            string callerName = stackFrame.GetMethod().DeclaringType.Name;
            return callerName == null
                ? throw new Exception("LogManager failed to get caller name when creating a logger.")
                : CreateLogger(callerName);
        }

        /// <summary>
        /// Creates a new logger with the specified name.
        /// </summary>
        public static Logger CreateLogger(string name)
        {
            // Create a new logger if there isn't one for this name already and return the requested logger
            // Use TryGetValue and discard the output because it's faster than ContainsKey
            if (LoggerDict.TryGetValue(name, out _) == false) LoggerDict.Add(name, new(name));
            return LoggerDict[name];
        }
    }
}
