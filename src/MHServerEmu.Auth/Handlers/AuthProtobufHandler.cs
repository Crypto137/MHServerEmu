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
            MessageBuffer messageBuffer = new(request.InputStream);

            switch ((FrontendProtocolMessage)messageBuffer.MessageId)
            {
                case FrontendProtocolMessage.LoginDataPB:       await OnLoginDataPB(request, response, messageBuffer); break;
                case FrontendProtocolMessage.PrecacheHeaders:   await OnPrecacheHeaders(request, response, messageBuffer); break;

                default: Logger.Warn($"HandleMessageAsync(): Unhandled {(FrontendProtocolMessage)messageBuffer.MessageId} [{messageBuffer.MessageId}]"); break;
            }
        }

        /// <summary>
        /// Handles a <see cref="LoginDataPB"/> message.
        /// </summary>
        private async Task<bool> OnLoginDataPB(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse, MessageBuffer messageBuffer)
        {
            LoginDataPB loginDataPB = messageBuffer.Deserialize<FrontendProtocolMessage>() as LoginDataPB;
            if (loginDataPB == null) return Logger.WarnReturn(false, $"OnLoginDataPB(): Failed to retrieve message");

            // Mask the end point name to prevent sensitive information from appearing in logs in needed
            string endPointName = HideSensitiveInformation
                ? httpRequest.RemoteEndPoint.ToStringMasked()
                : httpRequest.RemoteEndPoint.ToString();

#if DEBUG
            // Send a TOS popup when the client uses tos@test.com as email
            if (loginDataPB.EmailAddress == "tos@test.com")
            {
                var tosTicket = AuthTicket.CreateBuilder()
                    .SetSessionId(0)
                    .SetTosurl("http://localhost/tos")  // The client adds &locale=en_us to this url (or another locale code)
                    .Build();

                await HttpHelper.SendProtobufAsync(httpResponse, tosTicket, (int)AuthStatusCode.NeedToAcceptLegal);
                return true;
            }
#endif

            // Try to create a new session from the data we received
            PlayerManagerService playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
                return Logger.ErrorReturn(false, $"OnLoginDataPB(): Failed to connect to the player manager");

            AuthStatusCode statusCode = playerManager.OnLoginDataPB(loginDataPB, out AuthTicket ticket);
            
            // Respond with an error if session creation didn't succeed
            if (statusCode != AuthStatusCode.Success)
            {
                httpResponse.StatusCode = (int)statusCode;
                return Logger.InfoReturn(true, $"Authentication for the game client on {endPointName} failed ({statusCode})");
            }

            // Send an AuthTicket if we were able to create a session
            string machineId = loginDataPB.HasMachineId ? loginDataPB.MachineId : string.Empty;
            Logger.Info($"Sending AuthTicket for SessionId 0x{ticket.SessionId:X} to the game client on {endPointName}, machineId={machineId}");
            await HttpHelper.SendProtobufAsync(httpResponse, ticket);
            return true;
        }

        /// <summary>
        /// Handles a <see cref="PrecacheHeaders"/> message.
        /// </summary>
        private async Task<bool> OnPrecacheHeaders(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse, MessageBuffer messageBuffer)
        {
            // The client sends this message on startup
            Logger.Trace($"Received PrecacheHeaders message");
            await HttpHelper.SendProtobufAsync(httpResponse, PrecacheHeadersMessageResponse.DefaultInstance);
            return true;
        }
    }
}
