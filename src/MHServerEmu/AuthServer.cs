using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu
{
    public class AuthServer
    {
        private const string ServerHost = "localhost";

        private HttpListener _listener;
        private int _requestCount = 0;

        public AuthServer(int port)
        {
            string url = $"http://{ServerHost}:{port}/";

            // Create an http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://{ServerHost}:{port}/");
            _listener.Start();
            Console.WriteLine($"[AuthServer] Listening for connections on {url}");
        }

        public async void HandleIncomingConnections()
        {
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await _listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print request info
                Console.WriteLine($"[AuthServer] Received request #: {++_requestCount}");
                Console.WriteLine($"[AuthServer] Request user agent: {req.UserAgent}");
                Console.WriteLine($"[AuthServer] Sending AuthTicket message");

                // Prepare data
                byte[] authTicket = Gazillion.AuthTicket.CreateBuilder()
                    .SetSessionKey(ByteString.CopyFrom(Cryptography.AuthEncryptionKey))
                    .SetSessionToken(ByteString.CopyFrom(new byte[] { 0x00, 0x01, 0x02, 0x03 }))
                    .SetSessionId(12345678)
                    .SetFrontendServer("localhost")
                    .SetFrontendPort("8081")
                    .SetSuccess(true)
                    .Build().ToByteArray();

                // Write data to a buffer and send the response
                byte[] buffer;
                using (MemoryStream memoryStream = new())
                {
                    using (BinaryWriter binaryWriter = new(memoryStream))
                    {
                        binaryWriter.Write((byte)AuthMessage.AuthTicket);
                        binaryWriter.Write(Convert.ToByte(authTicket.Length));
                        binaryWriter.Write(authTicket);
                    }

                    buffer = memoryStream.ToArray();
                }

                resp.KeepAlive = true;
                resp.ContentType = "application/octet-stream";
                resp.ContentLength64 = buffer.Length;

                await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                resp.Close();
            }
        }
    }
}
