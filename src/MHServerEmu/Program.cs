using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Data;
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            PrintBanner();
            Console.ResetColor();

            if (ConfigManager.IsInitialized == false)
            {
                Logger.Fatal("Failed to initialize config");
                Console.ReadKey();
                return;
            }

            Logger.Info("MHServerEmu starting...");

            if (Database.IsInitialized == false)
            {
                Console.ReadKey();
                return;
            }

            _authServer = new AuthServer(8080);
            new Thread(() => _authServer.HandleIncomingConnections()).Start();

            _frontendServer = new FrontendServer(4306);

            while (true)
            {
                string input = Console.ReadLine();
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;
                else if (input.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    _frontendServer.Shutdown();
                else if (input.Equals("parse", StringComparison.OrdinalIgnoreCase))
                    PacketHelper.ParseServerMessagesFromAllPacketFiles();
            }

            _frontendServer.Shutdown();
        }

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
    }
}
