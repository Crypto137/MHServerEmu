using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Leaderboards
{
    public class Leaderboard
    {
        public ulong LeaderboardId { get; } // PrototypeGuid
        public LeaderboardPrototype Prototype { get; }
        public List<LeaderboardInstance> Instances { get; }
        public LeaderboardInstance ActiveInstance { get; protected set; }
        public bool IsActive { get; set; }

        public Leaderboard(LeaderboardPrototype proto)
        {
            Prototype = proto;
        }

        public LeaderboardInstance GetInstance(ulong instanceId)
        {
            return Instances.Find(instance => instance.InstanceId == instanceId);
        }

        public void OnUpdate(in LeaderboardQueue queue)
        {
            if (IsActive) ActiveInstance.OnUpdate(queue);
        }
    }
}
