using MHServerEmu.Common.Config.Containers;
using MHServerEmu.Common.Helpers;

namespace MHServerEmu.Common.Config
{
    /// <summary>
    /// Provides access to config value containers.
    /// </summary>
    public static class ConfigManager
    {
        public static bool IsInitialized { get; private set; }

        // Add new containers here as needed
        public static LoggingConfig Logging { get; private set; }
        public static FrontendConfig Frontend { get; private set; }
        public static AuthConfig Auth { get; private set; }
        public static PlayerManagerConfig PlayerManager { get; private set; }
        public static DefaultPlayerDataConfig DefaultPlayerData { get; private set; }
        public static GroupingManagerConfig GroupingManager { get; private set; }
        public static GameDataConfig GameData { get; private set; }
        public static GameOptionsConfig GameOptions { get; private set; }
        public static BillingConfig Billing { get; private set; }

        static ConfigManager()
        {
            string path = Path.Combine(FileHelper.ServerRoot, "Config.ini");

            if (File.Exists(path) == false)
            {
                // Write to console manually because loggers require config to initialize
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to initialize config");
                Console.ResetColor();
                IsInitialized = false;
                return;
            }

            // Store IniFile in an array to pass as a parameter to container constructors
            var parameters = new[] { new IniFile(path) };
            
            // Iterate through config container properties and construct them using reflection
            foreach (var property in typeof(ConfigManager).GetProperties())
            {
                if (property.PropertyType.IsSubclassOf(typeof(ConfigContainer)) == false) continue;

                // Get the constructor for the container and invoke it
                var ctor = property.PropertyType.GetConstructors().First();
                property.SetValue(null, ctor.Invoke(parameters));
            }

            IsInitialized = true;
        }
    }
}
