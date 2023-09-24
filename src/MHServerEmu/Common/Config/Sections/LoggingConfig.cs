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

        public bool EnableFile { get; }
        public bool FileIncludeTimestamps { get; }
        public Logger.Level FileMinLevel { get; }
        public Logger.Level FileMaxLevel { get; }

        public LoggingConfig(IniFile configFile)
        {
            EnableLogging = configFile.ReadBool(Section, nameof(EnableLogging));

            EnableConsole = configFile.ReadBool(Section, nameof(EnableConsole));
            ConsoleIncludeTimestamps = configFile.ReadBool(Section, nameof(ConsoleIncludeTimestamps));
            ConsoleMinLevel = (Logger.Level)configFile.ReadInt(Section, nameof(ConsoleMinLevel));
            ConsoleMaxLevel = (Logger.Level)configFile.ReadInt(Section, nameof(ConsoleMaxLevel));

            EnableFile = configFile.ReadBool(Section, nameof(EnableFile));
            FileIncludeTimestamps = configFile.ReadBool(Section, nameof(FileIncludeTimestamps));
            FileMinLevel = (Logger.Level)configFile.ReadInt(Section, nameof(FileMinLevel));
            FileMaxLevel = (Logger.Level)configFile.ReadInt(Section, nameof(FileMaxLevel));
        }
    }
}
