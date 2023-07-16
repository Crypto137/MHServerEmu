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
        public static AuthServer _authServer;
        public static FrontendServer _frontendServer;

        static void Main(string[] args)
        {
            Console.WriteLine("MHServerEmu starting...");
            Console.WriteLine();

            _authServer = new AuthServer(8080);
            new Thread(() => _authServer.HandleIncomingConnections()).Start();

            _frontendServer = new FrontendServer(8081);

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
    }
}
