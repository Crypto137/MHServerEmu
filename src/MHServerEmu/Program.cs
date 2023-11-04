using System.Globalization;
using MHServerEmu.Auth;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Common.Logging.Targets;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu
{
    class Program
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public static readonly DateTime StartupTime = DateTime.Now;

        public static FrontendServer FrontendServer { get; private set; }
        public static AuthServer AuthServer { get; private set; }

        public static Thread FrontendServerThread { get; private set; }
        public static Thread AuthServerThread { get; private set; }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;    // Watch for unhandled exceptions
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;         // Make sure thread culture is invariant

            PrintBanner();  

            // Initialize config and loggers before doing anything else
            if (ConfigManager.IsInitialized == false)
            {
                Console.ReadLine();
                return;
            }

            InitLoggers();

            Logger.Info("MHServerEmu starting...");

            // Initialize everything else and start the servers
            if (ProtocolDispatchTable.IsInitialized == false || GameDatabase.IsInitialized == false || AccountManager.IsInitialized == false)
            {
                Console.ReadLine();
                return;
            }

            // StartServers();

            // debug part
            // 7293929583592937434	Regions/HUBS/XaviersMansion/XaviersMansionRegion.prototype
            Logger.Debug($"Start Test");
            {
                Prototype proto = 7293929583592937434u.GetPrototype();
                RegionPrototype regionPrototype = new(proto);

                Logger.Debug($"XaviersMansionRegion.Level = {regionPrototype.Level}");
                Logger.Debug($"XaviersMansionRegion.PlayerLimit = {regionPrototype.PlayerLimit}");
                RegionGeneratorPrototype r = regionPrototype.RegionGenerator;
                if (r is StaticRegionGeneratorPrototype)
                    Logger.Debug($"XaviersMansionRegion.RegionGenerator is StaticRegionGeneratorPrototype");
                Logger.Debug($"XaviersMansionRegion.RegionGenerator\n.StaticAreas[0]\n.Area = {(r as StaticRegionGeneratorPrototype).StaticAreas[0].Area}");

                // 9142075282174842340	Regions/HUBRevamp/NPEAvengersTowerHUBRegion.prototype
                proto = 9142075282174842340u.GetPrototype();
                regionPrototype = new(proto);
                r = regionPrototype.RegionGenerator;
                if (r is SequenceRegionGeneratorPrototype)
                    Logger.Debug($"NPEAvengersTowerHUBRegion.RegionGenerator is SequenceRegionGeneratorPrototype");
                Logger.Debug($"NPEAvengersTowerHUBRegion.RegionGenerator\n.AreaSequence[0]\n.AreaChoices[0]\n.Area = {(r as SequenceRegionGeneratorPrototype).AreaSequence[0].AreaChoices[0].Area}");

                // 15546930156792977757	Regions/StoryRevamp/CH03Madripoor/CH0301MadripoorRegion.prototype
                proto = 15546930156792977757u.GetPrototype();
                regionPrototype = new(proto);
                r = regionPrototype.RegionGenerator;
                if (r is SequenceRegionGeneratorPrototype)
                    Logger.Debug($"CH0301MadripoorRegion.RegionGenerator is SequenceRegionGeneratorPrototype");
                Logger.Debug($"CH0301MadripoorRegion.RegionGenerator\n.AreaSequence[0]\n.ConnectedTo[0]\n.AreaChoices[0]\n.ConnectOn = {(r as SequenceRegionGeneratorPrototype).AreaSequence[0].ConnectedTo[0].AreaChoices[0].ConnectOn}");

                Type regions = typeof(RegionPrototypeId);
                Logger.Debug($"start load regions");
                foreach (ulong regionProtoId in Enum.GetValues(regions))
                {
                    proto = regionProtoId.GetPrototype();
                    regionPrototype = new(proto);
                    Logger.Debug($"region[{regionProtoId}].RegionName = {regionPrototype.RegionName}");
                }
                Logger.Debug($"end load");
            }
            Logger.Debug($"End Test");
            // Begin processing console input
            Logger.Info("Type '!commands' for a list of available commands");
            while (true)
            {
                string input = Console.ReadLine();
                CommandManager.Parse(input);
            }
        }

        public static void Shutdown()
        {
            if (AuthServer != null)
            {
                Logger.Info("Shutting down AuthServer...");
                AuthServer.Shutdown();
            }

            if (FrontendServer != null)
            {
                Logger.Info("Shutting down FrontendServer...");
                FrontendServer.Shutdown();
            }

            Environment.Exit(0);
        }

        public static string GetServerStatus()
        {
            return $"Server Status\nUptime: {DateTime.Now - StartupTime:hh\\:mm\\:ss}\nSessions: {FrontendServer.PlayerManagerService.SessionCount}";
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"  __  __ _    _  _____                          ______                 ");
            Console.WriteLine(@" |  \/  | |  | |/ ____|                        |  ____|                ");
            Console.WriteLine(@" | \  / | |__| | (___   ___ _ ____   _____ _ __| |__   _ __ ___  _   _ ");
            Console.WriteLine(@" | |\/| |  __  |\___ \ / _ \ '__\ \ / / _ \ '__|  __| | '_ ` _ \| | | |");
            Console.WriteLine(@" | |  | | |  | |____) |  __/ |   \ V /  __/ |  | |____| | | | | | |_| |");
            Console.WriteLine(@" |_|  |_|_|  |_|_____/ \___|_|    \_/ \___|_|  |______|_| |_| |_|\__,_|");
            Console.WriteLine();
            Console.ResetColor();
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;

            if (e.IsTerminating)
                Logger.FatalException(ex, "MHServerEmu terminating because of unhandled exception.");
            else
                Logger.ErrorException(ex, "Caught unhandled exception.");

            Console.ReadLine();
        }

        private static void InitLoggers()
        {
            LogManager.Enabled = ConfigManager.Logging.EnableLogging;

            // Attach console log target
            if (ConfigManager.Logging.EnableConsole)
                LogManager.AttachLogTarget(new ConsoleTarget(ConfigManager.Logging.ConsoleIncludeTimestamps,
                    ConfigManager.Logging.ConsoleMinLevel, ConfigManager.Logging.ConsoleMaxLevel));

            // Attach file log target
            if (ConfigManager.Logging.EnableFile)
                LogManager.AttachLogTarget(new FileTarget(ConfigManager.Logging.FileIncludeTimestamps,
                    ConfigManager.Logging.FileMinLevel, ConfigManager.Logging.FileMaxLevel,
                    $"MHServerEmu_{StartupTime:yyyy-dd-MM_HH.mm.ss}.log", false));
        }

        #region Server Control

        private static void StartServers()
        {
            StartFrontendServer();
            StartAuthServer();
        }

        private static bool StartFrontendServer()
        {
            if (FrontendServer != null) return false;

            FrontendServer = new FrontendServer();
            FrontendServerThread = new(FrontendServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            FrontendServerThread.Start();

            return true;
        }

        private static bool StartAuthServer()
        {
            if (AuthServer != null) return false;

            AuthServer = new(FrontendServer.PlayerManagerService);
            AuthServerThread = new(AuthServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            AuthServerThread.Start();

            return true;
        }

        #endregion
    }
}
