using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public class AuthServer
    {
        private enum ErrorCode
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

        private HttpListener _listener;

        public AuthServer(int port)
        {
            string url = $"http://{ServerHost}:{port}/";

            // Create an http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add(url);
            _listener.Start();

            new Thread(() => HandleIncomingConnections()).Start();

            Logger.Info($"AuthServer is listening on {url}...");
        }

        public async void HandleIncomingConnections()
        {
            while (true)
            {
                // Wait for a connection and get request and response from it
                HttpListenerContext ctx = await _listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if (req.UserAgent == "Secret Identity Studios Http Client")     // Ignore requests from other user agents
                {
                    if (req.HttpMethod == "POST")
                    {
                        // Parse message from POST
                        CodedInputStream stream = CodedInputStream.CreateInstance(req.InputStream);
                        GameMessage message = new((byte)stream.ReadRawVarint64(), stream.ReadRawBytes((int)stream.ReadRawVarint64()));

                        switch ((FrontendProtocolMessage)message.Id)
                        {
                            case FrontendProtocolMessage.LoginDataPB:
                                var loginDataPB = LoginDataPB.ParseFrom(message.Content);
                                byte[] authTicket;

                                if (CheckLoginDataPB(loginDataPB))  // check if LoginDataPB is valid
                                {
                                    authTicket = AuthTicket.CreateBuilder()
                                        .SetSessionKey(ByteString.CopyFrom(Cryptography.AuthEncryptionKey))
                                        .SetSessionToken(ByteString.CopyFrom(new byte[] { 0x00, 0x01, 0x02, 0x03 }))
                                        .SetSessionId(17323122570962387736)
                                        .SetFrontendServer("localhost")
                                        .SetFrontendPort("4306")
                                        .SetSuccess(true)
                                        .Build().ToByteArray();

                                    // Write data to a buffer and send the response
                                    byte[] buffer;
                                    using (MemoryStream memoryStream = new())
                                    {
                                        CodedOutputStream outputStream = CodedOutputStream.CreateInstance(memoryStream);
                                        outputStream.WriteRawVarint64((byte)AuthMessage.AuthTicket);
                                        outputStream.WriteRawVarint64((ulong)authTicket.Length);
                                        outputStream.WriteRawBytes(authTicket);
                                        outputStream.Flush();
                                        buffer = memoryStream.ToArray();
                                    }

                                    resp.KeepAlive = false;
                                    resp.ContentType = "application/octet-stream";
                                    resp.ContentLength64 = buffer.Length;

                                    await resp.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                                }
                                else
                                {
                                    Logger.Info("Authentication failed (LoginDataPB is invalid)");
                                    resp.StatusCode = (int)ErrorCode.IncorrectUsernameOrPassword1;
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
                else
                {
                    Logger.Warn($"Received {req.HttpMethod} from an unknown UserAgent: {req.UserAgent}");
                }

                resp.Close();
            }
        }

        private bool CheckLoginDataPB(LoginDataPB loginDataPB)
        {
            return true;    // TODO: actual checking
        }
    }
}
