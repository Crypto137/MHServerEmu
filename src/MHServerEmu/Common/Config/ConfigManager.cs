using MHServerEmu.Common.Config.Sections;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Common.Config
{
    public static class ConfigManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static bool IsInitialized { get; private set; }

        public static ServerConfig Server { get; }
        public static PlayerDataConfig PlayerData { get; }
        public static FrontendConfig Frontend { get; }
        public static GroupingManagerConfig GroupingManager { get; }
        public static GameOptionsConfig GameOptions { get; }

        static ConfigManager()
        {
            string path = $"{Directory.GetCurrentDirectory()}\\Config.ini";

            if (File.Exists(path))
            {
                IniFile configFile = new(path);

                Server = new(configFile);
                PlayerData = new(configFile);
                Frontend = new(configFile);
                GroupingManager = new(configFile);
                GameOptions = new(configFile);

                IsInitialized = true;
            }
            else
            {
                Server = new();         // initialize default server config so that logging still works
                Logger.Fatal("Failed to initialize config");
                IsInitialized = false;
            }
        }
    }
}
