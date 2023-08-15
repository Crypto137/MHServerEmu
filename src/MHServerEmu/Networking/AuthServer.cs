using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Frontend;

namespace MHServerEmu.Networking
{
    public class AuthServer
    {
        public enum ErrorCode
        {
            IncorrectUsernameOrPassword1 = 401,
            AccountBanned = 402,
            IncorrectUsernameOrPassword2 = 403,
            CouldNotReachAuthServer = 404,
            EmailNotVerified = 405,
            UnableToConnect1 = 406,
            NeedToAcceptLegal = 407,
            PatchRequired = 409,
            AccountArchived = 411,
            PasswordExpired = 412,
            UnableToConnect2 = 413,
            UnableToConnect3 = 414,
            UnableToConenct4 = 415,
            UnableToConnect5 = 416,
            AgeRestricted = 417,
            UnableToConnect6 = 418,
            TemporarilyUnavailable = 503
        }

        private static readonly Logger Logger = LogManager.CreateLogger();

        private const string ServerHost = "localhost";

        private FrontendService _frontendService;
        private HttpListener _listener;

        public AuthServer(int port, FrontendService frontendService)
        {
            _frontendService = frontendService;

            string url = $"http://{ServerHost}:{port}/";

            // Create an http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            new Thread(() => HandleIncomingConnections()).Start();

            Logger.Info($"AuthServer is listening on {url}...");
        }

        private async void HandleIncomingConnections()
        {
            while (true)
            {
                // Wait for a connection and get request and response from it
                HttpListenerContext context = await _listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.UserAgent == "Secret Identity Studios Http Client")     // Ignore requests from other user agents
                {
                    if (request.HttpMethod == "POST")
                        HandleMessage(request.InputStream, response);
                    else
                        Logger.Warn($"Received {request.HttpMethod} from the game client");
                }
                else
                {
                    Logger.Warn($"Received {request.HttpMethod} from an unknown UserAgent: {request.UserAgent}");
                }

                response.Close();
            }
        }

        private async void HandleMessage(Stream inputStream, HttpListenerResponse response)
        {
            // Parse message from POST
            CodedInputStream stream = CodedInputStream.CreateInstance(inputStream);
            GameMessage message = new((byte)stream.ReadRawVarint64(), stream.ReadRawBytes((int)stream.ReadRawVarint64()));

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.LoginDataPB:
                    var loginDataPB = LoginDataPB.ParseFrom(message.Content);
                    byte[] authTicket;

                    ClientSession session = _frontendService.CreateSessionFromLoginDataPB(loginDataPB, out ErrorCode? errorCode);

                    if (session != null)  // Send an AuthTicket if we were able to create a session
                    {
                        Logger.Info($"Sending AuthTicket for sessionId {session.Id}");

                        authTicket = AuthTicket.CreateBuilder()
                            .SetSessionKey(ByteString.CopyFrom(session.Key))
                            .SetSessionToken(ByteString.CopyFrom(session.Token))
                            .SetSessionId(session.Id)
                            .SetFrontendServer("localhost")
                            .SetFrontendPort("4306")
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

                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        Logger.Info($"Authentication failed ({errorCode})");
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
