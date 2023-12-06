using MHServerEmu.Billing;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Networking
{
    public class ServerManager : IGameService
    {
        public const string GameVersion = "1.52.0.1700";

        private static readonly Logger Logger = LogManager.CreateLogger();

        public FrontendServer FrontendServer { get; }

        public GroupingManagerService GroupingManagerService { get; }
        public PlayerManagerService PlayerManagerService { get; }
        public BillingService BillingService { get; }

        public long StartTime { get; }      // Used for calculating game time 

        public ServerManager(FrontendServer frontendServer)
        {
            FrontendServer = frontendServer;


            // Initialize services
            GroupingManagerService = new(this);
            PlayerManagerService = new(this);
            BillingService = new(this);

            StartTime = GetDateTime();
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerManagerHandshake)
                        PlayerManagerService.Handle(client, muxId, message);
                    else
                        FrontendServer.Handle(client, muxId, message);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, message);
                    else
                        FrontendServer.Handle(client, muxId, message);

                    break;

                default:
                    Logger.Warn($"Unhandled message on muxId {muxId}");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerManagerHandshake)
                        PlayerManagerService.Handle(client, muxId, messages);
                    else
                        FrontendServer.Handle(client, muxId, messages);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, messages);
                    else
                        FrontendServer.Handle(client, muxId, messages);

                    break;

                default:
                    Logger.Warn($"{messages.Count()} unhandled messages on muxId {muxId}");
                    break;
            }
        }

        public long GetDateTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() * 1000;
        }

        public long GetGameTime()
        {
            return GetDateTime() - StartTime;
        }
    }
}
