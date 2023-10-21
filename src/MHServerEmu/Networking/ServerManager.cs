using MHServerEmu.Billing;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Achievements;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Networking
{
    public class ServerManager : IGameService
    {
        public const string GameVersion = "1.52.0.1700";

        private static readonly Logger Logger = LogManager.CreateLogger();

        public AchievementDatabase AchievementDatabase { get; }

        public GameManager GameManager { get; }

        public FrontendService FrontendService { get; }
        public GroupingManagerService GroupingManagerService { get; }
        public PlayerManagerService PlayerManagerService { get; }
        public BillingService BillingService { get; }

        public long StartTime { get; }      // Used for calculating game time 

        public ServerManager()
        {
            // Initialize achievement database
            AchievementDatabase = new(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "CompressedAchievementDatabaseDump.bin")));

            // Initialize game manager
            GameManager = new(this);

            // Initialize services
            FrontendService = new(this);
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
                        FrontendService.Handle(client, muxId, message);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, message);
                    else
                        FrontendService.Handle(client, muxId, message);

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
                        FrontendService.Handle(client, muxId, messages);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, messages);
                    else
                        FrontendService.Handle(client, muxId, messages);

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
