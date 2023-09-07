using System.Diagnostics;

namespace MHServerEmu.Common.Logging
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
}
