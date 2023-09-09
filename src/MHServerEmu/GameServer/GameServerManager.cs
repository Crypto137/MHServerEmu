using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Achievements;
using MHServerEmu.GameServer.Billing;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.Games;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer
{
    public class GameServerManager : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public AchievementDatabase AchievementDatabase { get; }

        public GameManager GameManager { get; }

        public FrontendService FrontendService { get; }
        public GroupingManagerService GroupingManagerService { get; }
        public PlayerManagerService PlayerManagerService { get; }
        public BillingService BillingService { get; }

        public long StartTime { get; }      // Used for calculating game time 

        public GameServerManager()
        {
            // Initialize achievement database
            /*
            var achievementDatabaseDump = NetMessageAchievementDatabaseDump.ParseFrom(PacketHelper.LoadMessagesFromPacketFile("NetMessageAchievementDatabaseDump.bin")[0].Content);
            using (MemoryStream input = new(achievementDatabaseDump.CompressedAchievementDatabaseDump.ToByteArray()))
            using (MemoryStream output = new())
            using (InflaterInputStream inflater = new(input))
            {
                inflater.CopyTo(output);
                AchievementDatabase = new(AchievementDatabaseDump.ParseFrom(output.ToArray()));

                //File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "originalDump.bin"), achievementDatabaseDump.ToByteArray());
                //File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "newDump.bin"), achievementDatabase.ToNetMessageAchievementDatabaseDump().ToByteArray());
                //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Achievements.json"), JsonSerializer.Serialize(achievementDatabase, new JsonSerializerOptions() { WriteIndented = true }));
                //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "AchievementInfos.json"), JsonSerializer.Serialize(achievementDatabase.AchievementInfos));
                //File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "achievements.txt"), achievementDatabase.ToString());
                //File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "achievementStringBuffer.bin"), achievementDatabase.LocalizedAchievementStringBuffer.ToByteArray());
            }
            */

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
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                        PlayerManagerService.Handle(client, muxId, message);
                    else
                        FrontendService.Handle(client, muxId, message);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                        GroupingManagerService.Handle(client, muxId, message);
                    else
                        FrontendService.Handle(client, muxId, message);

                    break;

                default:
                    Logger.Warn($"Unhandled message on muxId {muxId}");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                        PlayerManagerService.Handle(client, muxId, messages);
                    else
                        FrontendService.Handle(client, muxId, messages);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                        GroupingManagerService.Handle(client, muxId, messages);
                    else
                        FrontendService.Handle(client, muxId, messages);

                    break;

                default:
                    Logger.Warn($"{messages.Length} unhandled messages on muxId {muxId}");
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
