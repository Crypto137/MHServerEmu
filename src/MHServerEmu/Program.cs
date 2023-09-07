using System.Globalization;
using MHServerEmu.Auth;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.Networking;

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
            // Watch for unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            PrintBanner();  

            if (ConfigManager.IsInitialized == false)
            {
                Console.ReadLine();
                return;
            }

            Logger.Info("MHServerEmu starting...");

            if (ProtocolDispatchTable.IsInitialized == false || GameDatabase.IsInitialized == false || AccountManager.IsInitialized == false)
            {
                Console.ReadLine();
                return;
            }

            StartServers();

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
            if (e.IsTerminating)
                Logger.Fatal($"MHServerEmu terminating because of unhandled exception: {e}");
            else
                Logger.Error($"Caught unhandled exception: {e}");

            Console.ReadLine();
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

            AuthServer = new(8080, FrontendServer.FrontendService);
            AuthServerThread = new(AuthServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            AuthServerThread.Start();

            return true;
        }

        #endregion
    }
}
