using Gazillion;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Leaderboards
{
    public class LeaderboardInfo
    {
        public LeaderboardPrototype Prototype { get; }
        public List<LeaderboardInstanceInfo> Instances { get; }

        public LeaderboardInfo(LeaderboardPrototype proto)
        {
            Prototype = proto;
            Instances = new();
        }
    }

    public class LeaderboardInstanceInfo
    {
        public PrototypeGuid LeaderboardId { get; set; }
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public DateTime ActivationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool Visible { get; set; }

        public void Update(LeaderboardInstanceInfo updateInstance)
        {
            if (State != updateInstance.State 
                || ActivationTime != updateInstance.ActivationTime 
                || ExpirationTime != updateInstance.ExpirationTime
                || Visible != Visible)
            {
                State = updateInstance.State;
                ActivationTime = updateInstance.ActivationTime;
                ExpirationTime = updateInstance.ExpirationTime;
                Visible = updateInstance.Visible;
            }
        }

        public NetMessageLeaderboardStateChange ToLeaderboardStateChange()
        {
            return NetMessageLeaderboardStateChange.CreateBuilder()
                .SetLeaderboardId((ulong)LeaderboardId)
                .SetInstanceId(InstanceId)
                .SetNewState(State)
                .SetActivationTimestamp(Clock.DateTimeToTimestamp(ActivationTime))
                .SetExpirationTimestamp(Clock.DateTimeToTimestamp(ExpirationTime))
                .SetVisible(Visible)
                .Build();
        }
    }

    public struct LeaderboardQueue
    {
        public PrototypeGuid LeaderboardId;
        public PrototypeGuid GameId;
        public ulong AvatarId;
        public ulong RuleId;
        public ulong Count;

        public LeaderboardQueue(in LeaderboardGuidKey key, int count)
        {
            LeaderboardId = key.LeaderboardGuid;
            GameId = (PrototypeGuid)key.PlayerGuid;
            AvatarId = (ulong)key.AvatarGuid;
            RuleId = (ulong)key.RuleGuid;
            Count = (ulong)count;
        }
    }
}
