using MHServerEmu.Core.Config;

namespace MHServerEmu.Core.Logging
{
    public class LoggingConfig : ConfigContainer
    {
        public bool EnableLogging { get; private set; } = true;
        public bool SynchronousMode { get; private set; } = false;
        public bool HideSensitiveInformation { get; private set; } = false;

        public bool EnableConsole { get; private set; } = true;
        public bool ConsoleIncludeTimestamps { get; private set; } = true;
        public LoggingLevel ConsoleMinLevel { get; private set; } = LoggingLevel.Trace;
        public LoggingLevel ConsoleMaxLevel { get; private set; } = LoggingLevel.Fatal;

        public bool EnableFile { get; private set; } = false;
        public bool FileIncludeTimestamps { get; private set; } = true;
        public LoggingLevel FileMinLevel { get; private set; } = LoggingLevel.Trace;
        public LoggingLevel FileMaxLevel { get; private set; } = LoggingLevel.Fatal;
    }
}
