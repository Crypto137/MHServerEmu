using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.Networking;

namespace MHServerEmu.Auth
{
    public class AuthServer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly string _url;
        private readonly FrontendService _frontendService;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private HttpListener _listener;

        public AuthServer(int port, FrontendService frontendService)
        {
            _url = $"http://localhost:{port}/";
            _frontendService = frontendService;
            _cancellationTokenSource = new();
        }

        public async void Run()
        {
            // Create an http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Logger.Info($"AuthServer is listening on {_url}...");

            while (true)
            {
                try
                {
                    // Wait for a connection and get request and response from it
                    HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(_cancellationTokenSource.Token);
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    if (request.UserAgent == "Secret Identity Studios Http Client")     // Ignore requests from other user agents
                    {
                        if (request.HttpMethod == "POST" && request.Url.LocalPath == "/Login/IndexPB")
                            HandleMessage(request, response);
                        else
                            Logger.Warn($"Received {request.HttpMethod} to {request.Url.LocalPath} from a game client on {request.RemoteEndPoint}");
                    }
                    else
                    {
                        Logger.Warn($"Received {request.HttpMethod} to {request.Url.LocalPath} from an unknown UserAgent on {request.RemoteEndPoint}. UserAgent information: {request.UserAgent}");
                    }

                    response.Close();
                }
                catch (TaskCanceledException e)
                {
                    return;     // Stop handling connections
                }
                catch (Exception e)
                {
                    Logger.Error($"Unhandled exception: {e}");
                }
            }
        }

        public void Shutdown()
        {
            if (_listener.IsListening == false) return;

            if (_listener != null)
            {
                // Cancel listening for context and close the listener
                _cancellationTokenSource.Cancel();
                _listener.Close();
                _listener = null;
            }
        }

        private async void HandleMessage(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Parse message from POST
            CodedInputStream stream = CodedInputStream.CreateInstance(request.InputStream);
            GameMessage message = new((byte)stream.ReadRawVarint64(), stream.ReadRawBytes((int)stream.ReadRawVarint64()));

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.LoginDataPB:
                    var loginDataPB = LoginDataPB.ParseFrom(message.Content);
                    ClientSession session = _frontendService.CreateSessionFromLoginDataPB(loginDataPB, out AuthErrorCode? errorCode);

                    if (session != null)  // Send an AuthTicket if we were able to create a session
                    {
                        Logger.Info($"Sending AuthTicket for sessionId {session.Id} to the game client on {request.RemoteEndPoint}");

                        byte[] authTicket = AuthTicket.CreateBuilder()
                            .SetSessionKey(ByteString.CopyFrom(session.Key))
                            .SetSessionToken(ByteString.CopyFrom(session.Token))
                            .SetSessionId(session.Id)
                            .SetFrontendServer(ConfigManager.Frontend.PublicAddress)
                            .SetFrontendPort(ConfigManager.Frontend.Port)
                            .SetPlatformTicket("")
                            .SetSuccess(true)
                            .Build().ToByteArray();

                        // Write data to a buffer and send the response
                        byte[] buffer;
                        using (MemoryStream memoryStream = new())
                        {
                            // The structure is like a mux packet, but without the 6 byte header
                            CodedOutputStream outputStream = CodedOutputStream.CreateInstance(memoryStream);
                            outputStream.WriteRawVarint64((byte)AuthMessage.AuthTicket);
                            outputStream.WriteRawVarint64((ulong)authTicket.Length);
                            outputStream.WriteRawBytes(authTicket);
                            outputStream.Flush();
                            buffer = memoryStream.ToArray();
                        }

                        response.KeepAlive = false;
                        response.ContentType = "application/octet-stream";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer);
                    }
                    else
                    {
                        Logger.Info($"Authentication for the game client on {request.RemoteEndPoint} failed ({errorCode})");
                        response.StatusCode = (int)errorCode;
                    }

                    break;

                case FrontendProtocolMessage.PrecacheHeaders:
                    // The client sends this message on startup
                    Logger.Trace($"Received PrecacheHeaders message");
                    break;

                default:
                    Logger.Warn($"Received unknown messageId {message.Id}");
                    break;
            }
        }
    }
}
