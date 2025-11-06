using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.Core.RateLimiting;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers
{
    public class ProtobufWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        private readonly TimeLeakyBucketCollection<string> _loginRateLimiter;

        public ProtobufWebHandler(bool enableLoginRateLimit, TimeSpan loginRateLimitCost, int loginRateLimitBurst)
        {
            if (enableLoginRateLimit)
                _loginRateLimiter = new(loginRateLimitCost, Math.Max(loginRateLimitBurst, 2));
        }

        protected override async Task Post(WebRequestContext context)
        {
            if (context.IsGameClientRequest == false)
            {
                context.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            IMessage message = context.ReadProtobuf<FrontendProtocolMessage>();

            switch (message)
            {
                case LoginDataPB loginDataPB:
                    await OnLoginDataPB(context, loginDataPB);
                    break;

                case PrecacheHeaders precacheHeaders:
                    await OnPrecacheHeaders(context, precacheHeaders);
                    break;

                default:
                    Logger.Warn($"Post(): Unhandled protobuf {message?.DescriptorForType.Name}");
                    context.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }
        }

        private async Task OnLoginDataPB(WebRequestContext context, LoginDataPB loginDataPB)
        {
            if (loginDataPB == null)
            {
                Logger.Warn($"OnLoginDataPB(): Failed to retrieve message");
                context.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            string ipAddress = context.GetIPAddress();

            // Hash the IP address to prevent it from appearing in logs if needed
            string ipAddressHandle = HideSensitiveInformation ? $"0x{HashHelper.Djb2(ipAddress):X4}" : ipAddress;

            if (_loginRateLimiter != null && _loginRateLimiter.AddTime(ipAddress) == false)
            {
                Logger.Warn($"OnLoginDataPB(): Rate limit exceeded for {ipAddressHandle}");
                context.StatusCode = (int)HttpStatusCode.TooManyRequests;
                return;
            }

            ServiceMessage.AuthResponse authResponse = await GameServiceTaskManager.Instance.AuthenticateAsync(loginDataPB);

            int statusCode = authResponse.StatusCode;
            AuthTicket authTicket = authResponse.AuthTicket;

            // Respond with an error if session creation didn't succeed
            if (statusCode != (int)HttpStatusCode.OK)
            {
                context.StatusCode = statusCode;
                Logger.Info($"Authentication for the game client on {ipAddressHandle} failed ({statusCode})");
                return;
            }

            // Send an AuthTicket if we were able to create a session
            string machineId = loginDataPB.HasMachineId ? loginDataPB.MachineId : string.Empty;
            Logger.Info($"Sending AuthTicket for SessionId 0x{authTicket.SessionId:X} to the game client on {ipAddressHandle}, machineId={machineId}");
            await context.SendAsync(authTicket);
        }

        private static async Task OnPrecacheHeaders(WebRequestContext context, PrecacheHeaders precacheHeaders)
        {
            Logger.Trace("Received PrecacheHeaders message");
            await context.SendAsync(PrecacheHeadersMessageResponse.DefaultInstance);
        }
    }
}
