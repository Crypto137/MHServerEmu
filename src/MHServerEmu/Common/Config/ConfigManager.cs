using MHServerEmu.Common.Config.Sections;

namespace MHServerEmu.Common.Config
{
    public static class ConfigManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }

        public static ServerConfig Server { get; }
        public static FrontendConfig Frontend { get; }

        static ConfigManager()
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Config.ini";

            if (File.Exists(path))
            {
                IniFile configFile = new(path);

                Server = LoadServerConfig(configFile);
                Frontend = LoadFrontendConfig(configFile);

                IsInitialized = true;
            }
            else
            {
                Server = new();         // initialize default server config so that logging still works
                IsInitialized = false;
            }
        }

        private static ServerConfig LoadServerConfig(IniFile configFile)
        {
            string section = "Server";

            bool enableTimestamps = configFile.ReadBool(section, "EnableTimestamps");

            return new(enableTimestamps);
        }

        private static FrontendConfig LoadFrontendConfig(IniFile configFile)
        {
            string section = "Frontend";

            bool simulateQueue = configFile.ReadBool(section, "SimulateQueue");
            ulong queuePlaceInLine = (ulong)configFile.ReadInt(section, "QueuePlaceInLine");
            ulong queueNumberOfPlayersInLine = (ulong)configFile.ReadInt(section, "QueueNumberOfPlayersInLine");

            return new(simulateQueue, queuePlaceInLine, queueNumberOfPlayersInLine);
        }
    }
}
