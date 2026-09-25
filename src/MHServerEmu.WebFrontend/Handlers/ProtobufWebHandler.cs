using System.Net;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.Core.RateLimiting;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers
{
    public class ProtobufWebHandler : WebHandler
    {
        private const string GameClientUserAgent = "Secret Identity Studios Http Client";

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TimeLeakyBucketCollection<string> _loginRateLimiter;

        public ProtobufWebHandler(bool enableLoginRateLimit, TimeSpan loginRateLimitCost, int loginRateLimitBurst)
        {
            if (enableLoginRateLimit)
                _loginRateLimiter = new(loginRateLimitCost, Math.Max(loginRateLimitBurst, 2));
        }

        protected override async Task Post(WebRequestContext context)
        {
            // Check user agent only on PC for now because console user agents appear to include firmware specific metadata (e.g. 13.52 for firmware version in the example below)
            // User-Agent: Secret Identity Studios Http Client libhttp/13.52 (PlayStation 4)
#if PLATFORM_TYPE_PC
            string userAgent = context.UserAgent;
            if (string.Equals(userAgent, GameClientUserAgent, StringComparison.InvariantCulture) == false)
            {
                context.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }
#endif
            IMessage message = context.ReadProtobuf<FrontendProtocolMessage>();

            switch (message)
            {
                case LoginDataPB loginDataPB:
                    await OnLoginDataPB(context, loginDataPB);
                    break;

                case LoginDataConsole loginDataConsole:
                    await OnLoginDataConsole(context, loginDataConsole);
                    break;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                case PrecacheHeaders precacheHeaders:
                    await OnPrecacheHeaders(context, precacheHeaders);
                    break;

                case NewsRequest newsRequest:
                    await OnNewsRequest(context, newsRequest);
                    break;
#endif

                default:
                    Verify.IsTrue(false, $"Unhandled protobuf {message?.DescriptorForType.Name}");
                    context.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }
        }

        private async Task OnLoginDataPB(WebRequestContext context, LoginDataPB loginDataPB)
        {
            if (!Verify.IsNotNull(loginDataPB))
            {
                context.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            string ipAddressHandle = context.GetIPAddressHandle(out string ipAddress);

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

        private async Task OnLoginDataConsole(WebRequestContext context, LoginDataConsole loginDataConsole)
        {
#if PLATFORM_TYPE_PC
            Logger.Warn("LoginDataConsole is not allowed on PC server builds");
            context.StatusCode = (int)HttpStatusCode.BadRequest;
#else
            Logger.Debug($"OnLoginDataConsole():\n{loginDataConsole}");

            // TODO: Use a custom PSN implementation to send email+password in the token field
            LoginDataPB.Builder loginDataPB = LoginDataPB.CreateBuilder()
                .SetEmailAddress("test1@test.com")
                .SetPassword("123");

            if (loginDataConsole.HasVersion)
                loginDataPB.SetVersion(loginDataConsole.Version);

            if (loginDataConsole.HasNoPersistenceThisSession)
                loginDataPB.SetNoPersistenceThisSession(loginDataConsole.NoPersistenceThisSession);

            // Currently unused fields:
            // 1.52+: serviceEntitlements, titleid
            // 1.53+: envid, consoletype, errorinfolog

            await OnLoginDataPB(context, loginDataPB.Build());
#endif
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private static async Task OnPrecacheHeaders(WebRequestContext context, PrecacheHeaders precacheHeaders)
        {
#if DEBUG
            Logger.Trace("Received PrecacheHeaders message");
#endif
            await context.SendAsync(PrecacheHeadersMessageResponse.DefaultInstance);
        }
#endif

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        private static async Task OnNewsRequest(WebRequestContext context, NewsRequest newsRequest)
        {
#if DEBUG
            Logger.Trace("Received NewsRequest message");
#endif
            await context.SendAsync(NewsMessageResponse.DefaultInstance);
        }
#endif
    }
}
