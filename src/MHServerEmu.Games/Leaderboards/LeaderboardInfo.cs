using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Leaderboards
{
    public class LeaderboardInfo
    {
        public ulong LeaderboardId { get; } // PrototypeGuid
        public LeaderboardPrototype Prototype { get; }
        public List<LeaderboardInstanceInfo> Instances { get; }
        public LeaderboardInstanceInfo ActiveInstance { get; protected set; }
        public bool IsActive { get; set; }
    }

    public class LeaderboardInstanceInfo
    {
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public TimeSpan ActivationTime { get; set; }
        public TimeSpan ExpirationTime { get; set; }
        public bool Visible { get; set; }

        public void OnUpdate(LeaderboardQueue queue)
        {
            throw new NotImplementedException();
        }
    }

    public struct LeaderboardQueue
    {
        public PrototypeGuid LeaderboardId;
        public ulong GameId;
        public ulong AvatarId;
        public long RuleId;
        public int Count;

        public LeaderboardQueue(in LeaderboardGuidKey key, int count)
        {
            LeaderboardId = key.LeaderboardGuid;
            GameId = key.PlayerGuid;
            AvatarId = (ulong)key.AvatarGuid;
            RuleId = key.RuleGuid;
            Count = count;
        }
    }
}
