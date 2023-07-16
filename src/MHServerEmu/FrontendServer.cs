using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu
{
    public class FrontendServer
    {
        private Socket _socket;
        private List<FrontendClient> _clientList = new();

        public FrontendServer(int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            _socket.Listen(10);

            BeginAccept();
        }

        private void BeginAccept()
        {
            Console.WriteLine("[Frontend] Waiting for connections");
            _socket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult result)
        {
            Console.WriteLine("[Frontend] Client connected");
            Socket clientSocket = _socket.EndAccept(result);
            FrontendClient client = new FrontendClient(clientSocket);
            _clientList.Add(client);
            new Thread(() => client.Run()).Start();
            BeginAccept();
        }

        public void Shutdown()
        {
            foreach (FrontendClient client in _clientList)
            {
                client.Disconnect();
            }
        }
    }
}
