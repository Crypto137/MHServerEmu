using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Represents a reference to a SubLeaderboard instance tracked by a MetaLeaderboard.
    /// </summary>
    public class MetaLeaderboardEntry
    {
        public PrototypeGuid SubLeaderboardId { get; }
        public ulong SubInstanceId { get; set; }
        public LeaderboardInstance SubInstance { get; set; }
        public LeaderboardRewardEntryPrototype[] Rewards { get; }

        public MetaLeaderboardEntry(PrototypeGuid subLeaderboardId, LeaderboardRewardEntryPrototype[] rewards)
        {
            SubLeaderboardId = subLeaderboardId;
            Rewards = rewards;
        }
    }
}
