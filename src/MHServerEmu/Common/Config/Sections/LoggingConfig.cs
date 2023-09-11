using MHServerEmu.Common.Logging;

namespace MHServerEmu.Common.Config.Sections
{
    public class LoggingConfig
    {
        private const string Section = "Logging";

        public bool EnableLogging { get; }

        public bool EnableConsole { get; }
        public bool ConsoleIncludeTimestamps { get; }
        public Logger.Level ConsoleMinLevel { get; }
        public Logger.Level ConsoleMaxLevel { get; }

        public LoggingConfig(IniFile configFile)
        {
            EnableLogging = configFile.ReadBool(Section, nameof(EnableLogging));

            EnableConsole = configFile.ReadBool(Section, nameof(EnableConsole));
            ConsoleIncludeTimestamps = configFile.ReadBool(Section, nameof(ConsoleIncludeTimestamps));
            ConsoleMinLevel = (Logger.Level)configFile.ReadInt(Section, nameof(ConsoleMinLevel));
            ConsoleMaxLevel = (Logger.Level)configFile.ReadInt(Section, nameof(ConsoleMaxLevel));
        }
    }
}
