using Gazillion;
using MHServerEmu.Core.Network;
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

        public void SortedInsertInstance(LeaderboardInstanceInfo instanceInfo)
        {
            // Keep instances sorted in descending order so that the most recent instances appear first.
            // This way when the instance list is queried we can quickly get the current instance + the last archived one.
            Instances.Add(instanceInfo);
            Instances.Sort((a, b) => b.InstanceId.CompareTo(a.InstanceId));
        }
    }

    public class LeaderboardInstanceInfo
    {
        public PrototypeGuid LeaderboardId { get; private set; }
        public ulong InstanceId { get; private set; }
        public LeaderboardState State { get; private set; }
        public DateTime ActivationTime { get; private set; }
        public DateTime ExpirationTime { get; private set; }
        public bool Visible { get; private set; }

        public LeaderboardInstanceInfo(in ServiceMessage.LeaderboardStateChange instanceInfo)
        {
            LeaderboardId = (PrototypeGuid)instanceInfo.LeaderboardId;
            InstanceId = instanceInfo.InstanceId;
            State = instanceInfo.State;
            ActivationTime = instanceInfo.ActivationTime;
            ExpirationTime = instanceInfo.ExpirationTime;
            Visible = instanceInfo.Visible;
        }

        public void Update(in ServiceMessage.LeaderboardStateChange updateInstance)
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

        public LeaderboardInstanceData ToProtobuf()
        {
            return LeaderboardInstanceData.CreateBuilder()
                .SetInstanceId(InstanceId)
                .SetState(State)
                .SetActivationTimestamp(Clock.DateTimeToTimestamp(ActivationTime))
                .SetExpirationTimestamp(Clock.DateTimeToTimestamp(ExpirationTime))
                .SetVisible(Visible)
                .Build();
        }
    }
}
