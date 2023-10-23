using MHServerEmu.Common.Config.Sections;

namespace MHServerEmu.Common.Config
{
    public static class ConfigManager
    {
        public static bool IsInitialized { get; private set; }

        public static LoggingConfig Logging { get; }
        public static FrontendConfig Frontend { get; }
        public static AuthConfig Auth { get; }
        public static PlayerManagerConfig PlayerManager { get; }
        public static DefaultPlayerDataConfig DefaultPlayerData { get; }
        public static GroupingManagerConfig GroupingManager { get; }
        public static GameOptionsConfig GameOptions { get; }
        public static BillingConfig Billing { get; }

        static ConfigManager()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Config.ini");

            if (File.Exists(path))
            {
                IniFile configFile = new(path);

                Logging = new(configFile);
                Frontend = new(configFile);
                Auth = new(configFile);
                PlayerManager = new(configFile);
                DefaultPlayerData = new(configFile);
                GroupingManager = new(configFile);
                GameOptions = new(configFile);
                Billing = new(configFile);

                IsInitialized = true;
            }
            else
            {
                // Write to console manually because loggers require config to initialize
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to initialize config");
                Console.ResetColor();
                IsInitialized = false;
            }
        }
    }
}
