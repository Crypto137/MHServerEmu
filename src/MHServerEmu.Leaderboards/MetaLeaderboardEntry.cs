using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class MetaLeaderboardEntry
    {
        public PrototypeGuid MetaLeaderboardId { get; }
        public ulong MetaInstanceId { get; set; }
        public LeaderboardInstance MetaInstance { get; set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; }

        public MetaLeaderboardEntry(PrototypeGuid metaLeaderboardId, LeaderboardRewardEntryPrototype[] rewards)
        {
            MetaLeaderboardId = metaLeaderboardId;
            Rewards = rewards;
        }
    }
}
