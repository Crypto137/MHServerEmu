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
            EnableLogging = configFile.ReadBool(Section, "EnableLogging");

            EnableConsole = configFile.ReadBool(Section, "EnableConsole");
            ConsoleIncludeTimestamps = configFile.ReadBool(Section, "ConsoleIncludeTimestamps");
            ConsoleMinLevel = (Logger.Level)configFile.ReadInt(Section, "ConsoleMinLevel");
            ConsoleMaxLevel = (Logger.Level)configFile.ReadInt(Section, "ConsoleMaxLevel");
        }
    }
}
