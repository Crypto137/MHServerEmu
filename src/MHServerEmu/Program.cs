using MHServerEmu.Common;
using MHServerEmu.Common.Commands;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.Networking;

namespace MHServerEmu
{
    class Program
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static AuthServer _authServer;
        public static FrontendServer _frontendServer;

        static void Main(string[] args)
        {
            PrintBanner();  

            if (ConfigManager.IsInitialized == false)
            {
                Console.ReadKey();
                return;
            }

            Logger.Info("MHServerEmu starting...");

            if (ProtocolDispatchTable.IsInitialized == false || GameDatabase.IsInitialized == false ||
                PropertyInfoTable.IsInitialized == false || AccountManager.IsInitialized == false)
            {
                Console.ReadKey();
                return;
            }

            _frontendServer = new FrontendServer();
            _authServer = new AuthServer(8080, _frontendServer.FrontendService);

            while (true)
            {
                string input = Console.ReadLine();
                CommandManager.Parse(input);
            }
        }

        public static void Shutdown()
        {
            _frontendServer.Shutdown();
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
    }
}
