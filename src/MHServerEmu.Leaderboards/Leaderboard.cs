using MHServerEmu.DatabaseAccess.Models;
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

        public Leaderboard(LeaderboardPrototype proto, DBLeaderboard dBLeaderboard)
        {
            Prototype = proto;

            var dbManager = LeaderboardDatabase.Instance.DBManager;
            var instanceList = dbManager.GetInstanceList(dBLeaderboard.LeaderboardId, proto.MaxArchivedInstances);
            foreach (var dbInstance in instanceList)
                AddInstance(dbInstance, true);

            if (dBLeaderboard.ActiveInstanceId != 0)
                SetActiveInstance(dBLeaderboard.ActiveInstanceId);                
        }

        private void SetActiveInstance(long activeInstanceId)
        {
            throw new NotImplementedException();
        }

        public void AddInstance(DBLeaderboardInstance dbInstance, bool loadEntries)
        {
            var instance = GetInstance((ulong)dbInstance.InstanceId);
            if (instance != null) return;

            instance = new(this, dbInstance);

            if (Prototype.Type == LeaderboardType.MetaLeaderboard)
                instance.InitMetaLeaderboardEntries(Prototype.MetaLeaderboardEntries);

            Instances.Add(instance);

            if (loadEntries) instance.LoadEntries();

            if (Prototype.Type == LeaderboardType.MetaLeaderboard)
                instance.LoadMetaInstances();
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
