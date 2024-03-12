using System.Collections.Specialized;
using System.Net;
using System.Web;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Auth.WebApi;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth
{
    /// <summary>
    /// Handles HTTP auth requests from clients.
    /// </summary>
    public class AuthServer : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly CancellationTokenSource _cts = new();
        private readonly WebApiHandler _webApiHandler = new();
        private readonly string _url;

        private HttpListener _listener;

        /// <summary>
        /// Constructs a new <see cref="AuthServer"/> instance.
        /// </summary>
        public AuthServer()
        {
            _url = $"http://{ConfigManager.Auth.Address}:{ConfigManager.Auth.Port}/";
        }

        #region IGameService Implementation

        /// <summary>
        /// Runs this <see cref="AuthServer"/> instance.
        /// </summary>
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
                    HttpListenerContext context = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                    HandleRequest(context.Request, context.Response);
                    context.Response.Close();
                }
                catch (TaskCanceledException) { return; }       // Stop handling connections
                catch (Exception e)
                {
                    Logger.Error($"Unhandled exception: {e}");
                }
            }
        }

        /// <summary>
        /// Stops listening and shuts down this <see cref="AuthServer"/> instance.
        /// </summary>
        public void Shutdown()
        {
            if (_listener == null) return;
            if (_listener.IsListening == false) return;

            // Cancel async tasks (listening for context)
            _cts.Cancel();

            // Close the listener
            _listener.Close();
            _listener = null;
        }

        public void Handle(ITcpClient client, GameMessage message)
        {
            Logger.Warn($"Handle(): AuthServer should not be handling messages from TCP clients!");
        }

        public void Handle(ITcpClient client, IEnumerable<GameMessage> messages)
        {
            Logger.Warn($"Handle(): AuthServer should not be handling messages from TCP clients!");
        }

        public string GetStatus()
        {
            if (_listener == null || _listener.IsListening == false)
                return "Not listening";
            
            return "Listening for requests";
        }

        #endregion

        /// <summary>
        /// Handles an <see cref="HttpListenerRequest"/>.
        /// </summary>
        private void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            bool requestIsFromGameClient = (request.UserAgent == "Secret Identity Studios Http Client");

            // We should be getting only GET and POST
            switch (request.HttpMethod)
            {
                case "GET":
                    if (request.Url.LocalPath == "/favicon.ico") return;     // Ignore favicon requests

                    // Web API get requests
                    if (requestIsFromGameClient == false && ConfigManager.Auth.EnableWebApi)
                    {
                        HandleWebApiRequest(request, response);
                        return;
                    }

                    break;

                case "POST":
                    // Client auth messages
                    if (requestIsFromGameClient && request.Url.LocalPath == "/Login/IndexPB")
                    {
                        HandleMessagesAsync(request, response);
                        return;
                    }
                    
                    // Web API post requests
                    if (requestIsFromGameClient == false && ConfigManager.Auth.EnableWebApi)
                    {
                        HandleWebApiRequest(request, response);
                        return;
                    }

                    break;
            }

            // Display a warning for unhandled requests
            string source = requestIsFromGameClient ? "a game client" : $"an unknown UserAgent ({request.UserAgent})";
            Logger.Warn($"HandleRequest(): Unhandled {request.HttpMethod} to {request.Url.LocalPath} from {source} on {request.RemoteEndPoint}");
        }

        /// <summary>
        /// Receives and handles <see cref="IMessage"/> instances received over HTTP.
        /// </summary>
        private async void HandleMessagesAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Mask end point name if needed
            string endPointName = ConfigManager.PlayerManager.HideSensitiveInformation
                ? request.RemoteEndPoint.ToStringMasked()
                : request.RemoteEndPoint.ToString();

            // Parse message from POST
            GameMessage message = new(CodedInputStream.CreateInstance(request.InputStream));

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.LoginDataPB:
                    if (message.TryDeserialize<LoginDataPB>(out var loginDataPB) == false) return;

                    // Send a TOS popup when the client uses tos@test.com as email
                    if (loginDataPB.EmailAddress.ToLower() == "tos@test.com")
                    {
                        var tosTicket = AuthTicket.CreateBuilder()
                            .SetSessionId(0)
                            .SetTosurl("http://localhost/tos")  // The client adds &locale=en_us to this url (or another locale code)
                            .Build();

                        await SendMessageAsync(response, tosTicket, (int)AuthStatusCode.NeedToAcceptLegal);
                        return;
                    }

                    // Try to create a new session from the data we received
                    var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
                    if (playerManager == null)
                    {
                        Logger.Error($"HandleMessagesAsync(): Failed to connect to the player manager");
                        return;
                    }

                    AuthStatusCode statusCode = playerManager.OnLoginDataPB(loginDataPB, out ClientSession session);

                    // Respond with an error if session creation didn't succeed
                    if (statusCode != AuthStatusCode.Success)
                    {
                        Logger.Info($"Authentication for the game client on {endPointName} failed ({statusCode})");
                        response.StatusCode = (int)statusCode;
                        return;
                    }

                    // Send an AuthTicket if we were able to create a session
                    Logger.Info($"Sending AuthTicket for sessionId {session.Id} to the game client on {endPointName}");

                    var ticket = AuthTicket.CreateBuilder()
                        .SetSessionKey(ByteString.CopyFrom(session.Key))
                        .SetSessionToken(ByteString.CopyFrom(session.Token))
                        .SetSessionId(session.Id)
                        .SetFrontendServer(ConfigManager.Frontend.PublicAddress)
                        .SetFrontendPort(ConfigManager.Frontend.Port)
                        .SetPlatformTicket("")
                        .SetHasnews(ConfigManager.PlayerManager.ShowNewsOnLogin)
                        .SetNewsurl(ConfigManager.PlayerManager.NewsUrl)
                        .SetSuccess(true)
                        .Build();

                    await SendMessageAsync(response, ticket);
                    break;

                case FrontendProtocolMessage.PrecacheHeaders:
                    // The client sends this message on startup
                    Logger.Trace($"Received PrecacheHeaders message");
                    await SendMessageAsync(response, PrecacheHeadersMessageResponse.DefaultInstance);
                    break;

                default:
                    Logger.Warn($"HandleMessagesAsync(): Unhandled messageId {message.Id}");
                    break;
            }
        }

        private async Task SendMessageAsync(HttpListenerResponse response, IMessage message, int statusCode = 200)
        {
            byte[] buffer = new GameMessage(message).Serialize();

            response.StatusCode = statusCode;
            response.KeepAlive = false;
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
        }

        private async void HandleWebApiRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Mask end point name if needed
            string endPointName = ConfigManager.PlayerManager.HideSensitiveInformation
                ? request.RemoteEndPoint.ToStringMasked()
                : request.RemoteEndPoint.ToString();

            byte[] buffer;
            NameValueCollection queryString = null;

            // Parse query string from POST requests
            if (request.HttpMethod == "POST")
                using (StreamReader reader = new(request.InputStream))
                    queryString = HttpUtility.ParseQueryString(reader.ReadToEnd());

            switch (request.Url.LocalPath)
            {
                default:
                    Logger.Warn($"HandleWebApiRequest(): Unhandled web API request\nRequest: {request.Url.LocalPath}\nRemoteEndPoint: {endPointName}\nUserAgent: {request.UserAgent}");
                    return;

                case "/AccountManagement/Create":
                    buffer = _webApiHandler.HandleRequest(WebApiRequest.AccountCreate, queryString);
                    break;

                case "/ServerStatus":
                    buffer = _webApiHandler.HandleRequest(WebApiRequest.ServerStatus, queryString);
                    break;
            }

            await response.OutputStream.WriteAsync(buffer);
        }
    }
}
