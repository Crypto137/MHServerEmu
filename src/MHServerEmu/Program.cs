using System.Globalization;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;
using MHServerEmu.Common.Logging.Targets;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu
{
    class Program
    {
#if DEBUG
        public const string BuildConfiguration = "Debug";
#elif RELEASE
        public const string BuildConfiguration = "Release";
#endif

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly string VersionInfo = $"Version {AssemblyHelper.GetAssemblyInformationalVersion()} | {AssemblyHelper.ParseAssemblyBuildTime():yyyy.MM.dd HH:mm:ss} UTC | {BuildConfiguration}";
        public static readonly DateTime StartupTime = DateTime.Now;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;    // Watch for unhandled exceptions
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;         // Make sure thread culture is invariant

            Console.Title = $"MHServerEmu ({VersionInfo})";

            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintBanner();
            PrintVersionInfo();
            Console.ResetColor();

            // Initialize config and loggers before doing anything else
            if (ConfigManager.IsInitialized == false)
            {
                Console.ReadLine();
                return;
            }

            InitLoggers();

            Logger.Info("MHServerEmu starting...");

            // Initialize everything else and start the servers
            if (InitSystems() == false)
            {
                Console.ReadLine();
                return;
            }

            ServerManager.Instance.Initialize();
            ServerManager.Instance.StartServers();

            // Begin processing console input
            Logger.Info("Type '!commands' for a list of available commands");
            while (true)
            {
                string input = Console.ReadLine();
                CommandManager.Parse(input);
            }
        }

        /// <summary>
        /// Shuts down all servers and exits the application.
        /// </summary>
        public static void Shutdown()
        {
            ServerManager.Instance.Shutdown();
            Environment.Exit(0);
        }
        
        /// <summary>
        /// Prints a fancy ASCII banner to console.
        /// </summary>
        private static void PrintBanner()
        {
            Console.WriteLine(@"  __  __ _    _  _____                          ______                 ");
            Console.WriteLine(@" |  \/  | |  | |/ ____|                        |  ____|                ");
            Console.WriteLine(@" | \  / | |__| | (___   ___ _ ____   _____ _ __| |__   _ __ ___  _   _ ");
            Console.WriteLine(@" | |\/| |  __  |\___ \ / _ \ '__\ \ / / _ \ '__|  __| | '_ ` _ \| | | |");
            Console.WriteLine(@" | |  | | |  | |____) |  __/ |   \ V /  __/ |  | |____| | | | | | |_| |");
            Console.WriteLine(@" |_|  |_|_|  |_|_____/ \___|_|    \_/ \___|_|  |______|_| |_| |_|\__,_|");
            Console.WriteLine();
        }

        /// <summary>
        /// Prints formatted version info to console.
        /// </summary>
        private static void PrintVersionInfo()
        {
            Console.WriteLine($"\t{VersionInfo}");
            Console.WriteLine();
        }

        /// <summary>
        /// Handles unhandled exceptions.
        /// </summary>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            if (e.IsTerminating)
            {
                Logger.FatalException(ex, "MHServerEmu terminating because of unhandled exception.");
                ServerManager.Instance.Shutdown();
            }
            else
            {
                Logger.ErrorException(ex, "Caught unhandled exception.");
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Initializes log targets.
        /// </summary>
        private static void InitLoggers()
        {
            LogManager.Enabled = ConfigManager.Logging.EnableLogging;

            // Attach console log target
            if (ConfigManager.Logging.EnableConsole)
                LogManager.AttachTarget(new ConsoleTarget(ConfigManager.Logging.ConsoleIncludeTimestamps,
                    ConfigManager.Logging.ConsoleMinLevel, ConfigManager.Logging.ConsoleMaxLevel));

            // Attach file log target
            if (ConfigManager.Logging.EnableFile)
                LogManager.AttachTarget(new FileTarget(ConfigManager.Logging.FileIncludeTimestamps,
                    ConfigManager.Logging.FileMinLevel, ConfigManager.Logging.FileMaxLevel,
                    $"MHServerEmu_{StartupTime:yyyy-dd-MM_HH.mm.ss}.log", false));

            if (ConfigManager.Logging.SynchronousMode)
                Logger.Debug($"Synchronous logging enabled");
        }

        /// <summary>
        /// Initializes systems needed to run the servers.
        /// </summary>
        private static bool InitSystems()
        {
            return PakFileSystem.Instance.Initialize()
                && ProtocolDispatchTable.IsInitialized
                && GameDatabase.IsInitialized
                && LiveTuningManager.IsInitialized
                && AccountManager.IsInitialized;
        }
    }
}
