using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.ProtocolBuffers;
using System.Security.AccessControl;

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

            Logger.Info("MHServerEmu starting...");

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
