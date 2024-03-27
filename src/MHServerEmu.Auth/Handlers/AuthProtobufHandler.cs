using System.Net;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth.Handlers
{
    /// <summary>
    /// Handler for <see cref="IMessage"/> instances sent to the <see cref="AuthServer"/>.
    /// </summary>
    public class AuthProtobufHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        /// <summary>
        /// Receives and handles an <see cref="IMessage"/>.
        /// </summary>
        public async Task HandleMessageAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            MessagePackage message = new(CodedInputStream.CreateInstance(request.InputStream));
            message.Protocol = typeof(FrontendProtocolMessage);

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.LoginDataPB:       await OnLoginDataPB(request, response, message); break;
                case FrontendProtocolMessage.PrecacheHeaders:   await OnPrecacheHeaders(request, response, message); break;

                default: Logger.Warn($"HandleMessageAsync(): Unhandled {(FrontendProtocolMessage)message.Id} [{message.Id}]"); break;
            }
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> instance as an <see cref="HttpListenerResponse"/>.
        /// </summary>
        private async Task SendMessageAsync(IMessage message, HttpListenerResponse response, int statusCode = 200)
        {
            byte[] buffer = new MessagePackage(message).Serialize();

            response.StatusCode = statusCode;
            response.KeepAlive = false;
            response.ContentType = "application/octet-stream";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer);
        }

        #region Message Handling

        /// <summary>
        /// Handles a <see cref="LoginDataPB"/> message.
        /// </summary>
        private async Task<bool> OnLoginDataPB(HttpListenerRequest request, HttpListenerResponse response, MessagePackage message)
        {
            var loginDataPB = message.Deserialize() as LoginDataPB;
            if (loginDataPB == null) return Logger.WarnReturn(false, $"OnLoginDataPB(): Failed to retrieve message");

            // Mask the end point name to prevent sensitive information from appearing in logs in needed
            string endPointName = HideSensitiveInformation
                ? request.RemoteEndPoint.ToStringMasked()
                : request.RemoteEndPoint.ToString();

            // Send a TOS popup when the client uses tos@test.com as email
            if (loginDataPB.EmailAddress.ToLower() == "tos@test.com")
            {
                var tosTicket = AuthTicket.CreateBuilder()
                    .SetSessionId(0)
                    .SetTosurl("http://localhost/tos")  // The client adds &locale=en_us to this url (or another locale code)
                    .Build();

                await SendMessageAsync(tosTicket, response, (int)AuthStatusCode.NeedToAcceptLegal);
                return true;
            }

            // Try to create a new session from the data we received
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return Logger.ErrorReturn(false, $"OnLoginDataPB(): Failed to connect to the player manager");

            AuthStatusCode statusCode = playerManager.OnLoginDataPB(loginDataPB, out AuthTicket ticket);
            
            // Respond with an error if session creation didn't succeed
            if (statusCode != AuthStatusCode.Success)
            {
                response.StatusCode = (int)statusCode;
                return Logger.InfoReturn(true, $"Authentication for the game client on {endPointName} failed ({statusCode})");
            }

            // Send an AuthTicket if we were able to create a session
            Logger.Info($"Sending AuthTicket for sessionId 0x{ticket.SessionId:X} to the game client on {endPointName}");
            await SendMessageAsync(ticket, response);
            return true;
        }

        /// <summary>
        /// Handles a <see cref="PrecacheHeaders"/> message.
        /// </summary>
        private async Task<bool> OnPrecacheHeaders(HttpListenerRequest request, HttpListenerResponse response, MessagePackage message)
        {
            // The client sends this message on startup
            Logger.Trace($"Received PrecacheHeaders message");
            await SendMessageAsync(PrecacheHeadersMessageResponse.DefaultInstance, response);
            return true;
        }

        #endregion
    }
}
