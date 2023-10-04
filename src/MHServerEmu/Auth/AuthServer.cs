using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Auth.WebApi;
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
        private readonly WebApiHandler _webApiHandler;

        private HttpListener _listener;

        public AuthServer(int port, FrontendService frontendService)
        {
            _url = $"http://localhost:{port}/";
            _frontendService = frontendService;
            _cancellationTokenSource = new();
            _webApiHandler = new();
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
                    // Wait for a connection, and handle the request
                    HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(_cancellationTokenSource.Token);
                    HandleRequest(context.Request, context.Response);
                    context.Response.Close();
                }
                catch (TaskCanceledException)
                {
                    return;     // Stop handling connections
                }
                catch (Exception e)
                {
                    Logger.Error($"Unhandled exception: {e}");
                }
            }
        }

        /// <summary>
        /// Stops listening and shuts down the auth server.
        /// </summary>
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

        /// <summary>
        /// Handles HTTP request.
        /// </summary>
        /// <param name="request">HTTP listener request.</param>
        /// <param name="response">HTTP listener response.</param>
        private void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            bool requestIsFromGameClient = (request.UserAgent == "Secret Identity Studios Http Client");

            switch (request.HttpMethod)
            {
                case "GET":
                    if (request.Url.LocalPath == "/favicon.ico") return;     // Ignore favicon requests

                    // We shouldn't be getting GET requests from game clients
                    if (requestIsFromGameClient)
                        Logger.Warn($"Received {request.HttpMethod} to {request.Url.LocalPath} from a game client on {request.RemoteEndPoint}");
                    else if (ConfigManager.WebApi.EnableWebApi)
                        HandleWebApiRequest(request, response);     // This is a potential web API request

                    break;

                case "POST":
                    // Accept POST requests only from game clients
                    if (requestIsFromGameClient)
                        HandleMessage(request, response);
                    else
                        Logger.Warn($"Received {request.HttpMethod} to {request.Url.LocalPath} from an unknown UserAgent on {request.RemoteEndPoint}. UserAgent information: {request.UserAgent}");

                    break;

                default:
                    // We shouldn't be getting any other request methods
                    string source = requestIsFromGameClient ? "a game client" : $"an unknown UserAgent ({request.UserAgent})";
                    Logger.Warn($"Received {request.HttpMethod} to {request.Url.LocalPath} from {source} on {request.RemoteEndPoint}");
                    break;
            }
        }

        /// <summary>
        /// Handles protobuf message received over HTTP.
        /// </summary>
        /// <param name="request">HTTP listener request.</param>
        /// <param name="response">HTTP listener response.</param>
        private async void HandleMessage(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Parse message from POST
            GameMessage message = new(CodedInputStream.CreateInstance(request.InputStream));

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.LoginDataPB:
                    var loginDataPB = LoginDataPB.ParseFrom(message.Payload);

                    // Send a TOS popup when the client uses tos@test.com as email
                    if (loginDataPB.EmailAddress.ToLower() == "tos@test.com")
                    {
                        SendTosPopup(response);
                        return;
                    }

                    // Try to create a new session from the data we received
                    AuthStatusCode statusCode = _frontendService.TryCreateSessionFromLoginDataPB(loginDataPB, out ClientSession session);

                    // Respond with an error if session creation didn't succeed
                    if (statusCode != AuthStatusCode.Success)
                    {
                        Logger.Info($"Authentication for the game client on {request.RemoteEndPoint} failed ({statusCode})");
                        response.StatusCode = (int)statusCode;
                        return;
                    }

                    // Send an AuthTicket if we were able to create a session
                    Logger.Info($"Sending AuthTicket for sessionId {session.Id} to the game client on {request.RemoteEndPoint}");

                    // Create a new AuthTicket, write it to a buffer, and send the response
                    byte[] buffer = new GameMessage(AuthTicket.CreateBuilder()
                        .SetSessionKey(ByteString.CopyFrom(session.Key))
                        .SetSessionToken(ByteString.CopyFrom(session.Token))
                        .SetSessionId(session.Id)
                        .SetFrontendServer(ConfigManager.Frontend.PublicAddress)
                        .SetFrontendPort(ConfigManager.Frontend.Port)
                        .SetPlatformTicket("")
                        .SetHasnews(ConfigManager.Frontend.ShowNewsOnLogin)
                        .SetNewsurl(ConfigManager.Frontend.NewsUrl)
                        .SetSuccess(true)
                        .Build()).Encode();

                    response.KeepAlive = false;
                    response.ContentType = "application/octet-stream";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer);

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

        private async void SendTosPopup(HttpListenerResponse response)
        {
            byte[] buffer = new GameMessage(AuthTicket.CreateBuilder()
                .SetSessionId(0)
                .SetTosurl("http://localhost/tos")  // The client adds &locale=en_us to this url (or another locale code)
                .Build()).Encode();

            response.StatusCode = (int)AuthStatusCode.NeedToAcceptLegal;
            response.KeepAlive = false;
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }

        private async void HandleWebApiRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            byte[] buffer;

            switch (request.Url.LocalPath)
            {
                default:
                    Logger.Warn($"Received unknown web API request:\nRequest: {request.Url.LocalPath}\nRemoteEndPoint: {request.RemoteEndPoint}\nUserAgent: {request.UserAgent}");
                    return;

                case "/AccountManagement/Create":
                    buffer = _webApiHandler.HandleRequest(WebApiRequest.AccountCreate, request.QueryString);
                    break;
            }

            await response.OutputStream.WriteAsync(buffer);
        }
    }
}
