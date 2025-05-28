using System.Globalization;
using MHServerEmu.Auth;
using MHServerEmu.Billing;
using MHServerEmu.Commands;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Logging.Targets;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Json;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Grouping;
using MHServerEmu.Leaderboards;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu
{
    public class ServerApp
    {
#if DEBUG
        public const string BuildConfiguration = "Debug";
#elif RELEASE
        public const string BuildConfiguration = "Release";
#endif

        public static readonly string VersionInfo = $"Version {AssemblyHelper.GetAssemblyInformationalVersion()} | {AssemblyHelper.ParseAssemblyBuildTime():yyyy.MM.dd HH:mm:ss} UTC | {BuildConfiguration}";

        private static readonly Logger Logger = LogManager.CreateLogger();
        private bool _isRunning = false;

        public static ServerApp Instance { get; } = new();
        public DateTime StartupTime { get; private set; }

        private ServerApp() { }

        public void Run()
        {
            // Prevent duplicate runs
            if (_isRunning) throw new InvalidOperationException();
            _isRunning = true;

            StartupTime = DateTime.Now;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintBanner();
            PrintVersionInfo();
            Console.ResetColor();

            // Init loggers before anything else
            InitLoggers();

            Logger.Info("MHServerEmu starting...");

            // Our encoding is not going to work unless we are running on a little-endian system
            if (BitConverter.IsLittleEndian == false)
            {
                Logger.Fatal("This computer's architecture uses big-endian byte order, which is not compatible with MHServerEmu.");
                Console.ReadLine();
                return;
            }

            // Initialize everything else and start the servers
            if (InitSystems() == false)
            {
                Console.ReadLine();
                return;
            }

            // Initialize the command system
            CommandManager.Instance.SetClientOutput(new FrontendClientChatOutput());
            ICommandParser.Instance = new CommandParser();

            // Create and register game services
            ServerManager serverManager = ServerManager.Instance;
            serverManager.Initialize();

            serverManager.RegisterGameService(new FrontendServer(), ServerType.FrontendServer);
            serverManager.RegisterGameService(new AuthServer(), ServerType.AuthServer);
            serverManager.RegisterGameService(new PlayerManagerService(), ServerType.PlayerManager);
            serverManager.RegisterGameService(new GroupingManagerService(), ServerType.GroupingManager);
            serverManager.RegisterGameService(new GameInstanceService(), ServerType.GameInstanceServer);
            serverManager.RegisterGameService(new BillingService(), ServerType.Billing);
            serverManager.RegisterGameService(new LeaderboardService(), ServerType.Leaderboard);

            serverManager.RunServices();

            // Begin processing console input
            Logger.Info("Type '!commands' for a list of available commands");
            while (true)
            {
                string input = Console.ReadLine();
                CommandManager.Instance.TryParse(input);
            }
        }

        /// <summary>
        /// Shuts down all services and exits the application.
        /// </summary>
        public void Shutdown()
        {
            ServerManager.Instance.ShutdownServices();
            Console.ReadLine();
            Environment.Exit(0);
        }

        /// <summary>
        /// Prints a fancy ASCII banner to console.
        /// </summary>
        private void PrintBanner()
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
        private void PrintVersionInfo()
        {
            Console.WriteLine($"\t{VersionInfo}");
            Console.WriteLine();
        }

        /// <summary>
        /// Handles unhandled exceptions.
        /// </summary>
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            if (e.IsTerminating)
            {
                DateTime now = DateTime.Now;

                string crashReportDir = Path.Combine(FileHelper.ServerRoot, "CrashReports");
                if (Directory.Exists(crashReportDir) == false)
                    Directory.CreateDirectory(crashReportDir);

                string crashReportFilePath = Path.Combine(crashReportDir, $"ServerCrash_{now.ToString(FileHelper.FileNameDateFormat)}.txt");

                using (StreamWriter writer = new(crashReportFilePath))
                {
                    writer.WriteLine($"{VersionInfo}\n");
                    writer.WriteLine($"Local Server Time: {now:yyyy.MM.dd HH:mm:ss.fff}\n");
                    writer.WriteLine($"Exception:\n{exception}\n");
                    writer.WriteLine($"Server Status:\n{ServerManager.Instance.GetServerStatus(true)}\n");
                }

                Logger.FatalException(exception, $"MHServerEmu terminating because of unhandled exception, report saved to {crashReportFilePath}");
                ServerManager.Instance.ShutdownServices();
            }
            else
            {
                Logger.ErrorException(exception, "Caught unhandled exception.");
            }

            Console.ReadLine();
        }

        /// <summary>
        /// Initializes log targets.
        /// </summary>
        private void InitLoggers()
        {
            var config = ConfigManager.Instance.GetConfig<LoggingConfig>();

            LogManager.Enabled = config.EnableLogging;

            // Attach console log target
            if (config.EnableConsole)
            {
                ConsoleTarget target = new(config.GetConsoleSettings());
                LogManager.AttachTarget(target);
            }

            // Attach file log target
            if (config.EnableFile)
            {
                FileTarget target = new(config.GetFileSettings(), $"MHServerEmu_{StartupTime.ToString(FileHelper.FileNameDateFormat)}", config.FileSplitOutput, false);
                LogManager.AttachTarget(target);
            }

            if (config.SynchronousMode)
                Logger.Debug($"Synchronous logging enabled");
        }

        /// <summary>
        /// Initializes systems needed to run the servers.
        /// </summary>
        private bool InitSystems()
        {
            // JsonDBManager saves a single account in a JSON file
            var config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
            IDBManager.Instance = config.UseJsonDBManager ? JsonDBManager.Instance : SQLiteDBManager.Instance;

            return PakFileSystem.Instance.Initialize()
                && ProtocolDispatchTable.Instance.Initialize()
                && GameDatabase.IsInitialized
                && LiveTuningManager.Instance.Initialize()
                && IDBManager.Instance.Initialize()
                && AccountManager.Initialize();
        }
    }
}
