using System.Collections.Specialized;
using System.Net;
using System.Web;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Auth.WebApi;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth
{
    public class AuthServer
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly WebApiHandler _webApiHandler = new();
        private readonly string _url;

        private HttpListener _listener;

        public AuthServer()
        {
            _url = $"http://{ConfigManager.Auth.Address}:{ConfigManager.Auth.Port}/";
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
                catch (TaskCanceledException) { return; }       // Stop handling connections
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
            if (_listener == null) return;
            if (_listener.IsListening == false) return;

            // Cancel async tasks (listening for context)
            _cancellationTokenSource.Cancel();

            // Close the listener
            _listener.Close();
            _listener = null;
        }

        /// <summary>
        /// Handles HTTP request.
        /// </summary>
        /// <param name="request">HTTP listener request.</param>
        /// <param name="response">HTTP listener response.</param>
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
                        HandleMessage(request, response);
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
            Logger.Warn($"Received unhandled {request.HttpMethod} to {request.Url.LocalPath} from {source} on {request.RemoteEndPoint}");
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
                    if (message.TryDeserialize<LoginDataPB>(out var loginDataPB) == false) return;

                    // Send a TOS popup when the client uses tos@test.com as email
                    if (loginDataPB.EmailAddress.ToLower() == "tos@test.com")
                    {
                        SendTosPopup(response);
                        return;
                    }

                    // Try to create a new session from the data we received
                    AuthStatusCode statusCode = ServerManager.Instance.PlayerManagerService.OnLoginDataPB(loginDataPB, out ClientSession session);

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
                        .SetHasnews(ConfigManager.PlayerManager.ShowNewsOnLogin)
                        .SetNewsurl(ConfigManager.PlayerManager.NewsUrl)
                        .SetSuccess(true)
                        .Build()).Serialize();

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
                .Build()).Serialize();

            response.StatusCode = (int)AuthStatusCode.NeedToAcceptLegal;
            response.KeepAlive = false;
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }

        private async void HandleWebApiRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            byte[] buffer;
            NameValueCollection queryString = null;

            // Parse query string from POST requests
            if (request.HttpMethod == "POST")
                using (StreamReader reader = new(request.InputStream))
                    queryString = HttpUtility.ParseQueryString(reader.ReadToEnd());

            switch (request.Url.LocalPath)
            {
                default:
                    Logger.Warn($"Received unknown web API request:\nRequest: {request.Url.LocalPath}\nRemoteEndPoint: {request.RemoteEndPoint}\nUserAgent: {request.UserAgent}");
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
