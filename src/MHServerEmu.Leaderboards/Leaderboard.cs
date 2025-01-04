using Gazillion;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Leaderboards
{
    public class Leaderboard
    {
        private readonly object _lock = new object();
        public PrototypeGuid LeaderboardId { get; }
        public LeaderboardPrototype Prototype { get; }
        public List<LeaderboardInstance> Instances { get; }
        public LeaderboardInstance ActiveInstance { get; protected set; }
        public bool IsActive { get => ActiveInstance != null && ActiveInstance.State == LeaderboardState.eLBS_Active; }
        public bool CanReset { get => Prototype != null && Prototype.ResetFrequency != LeaderboardResetFrequency.NeverReset; }
        public LeaderboardScheduler Scheduler { get; protected set; }

        public Leaderboard(LeaderboardPrototype proto, DBLeaderboard dbLeaderboard)
        {
            Prototype = proto;
            LeaderboardId = (PrototypeGuid)dbLeaderboard.LeaderboardId;
            Instances = new();

            Scheduler = new();
            if (CanReset)
            {
                Scheduler.InitFromProto(proto);
                Scheduler.Initialize(dbLeaderboard);
            }

            var dbManager = LeaderboardDatabase.Instance.DBManager;
            var instanceList = dbManager.GetInstances(dbLeaderboard.LeaderboardId, proto.MaxArchivedInstances);
            foreach (var dbInstance in instanceList)
                AddInstance(dbInstance, true);

            if (dbLeaderboard.ActiveInstanceId != 0)
                ActiveInstance = GetInstance((ulong)dbLeaderboard.ActiveInstanceId);
        }

        public static ulong GenInstanceId(PrototypeGuid leaderboardId)
        {
            return ((ulong)leaderboardId & 0xFFFFFFFF00000000UL) | 1UL;
        }

        public bool SetActiveInstance(ulong activeInstanceId, LeaderboardState state, bool dbUpdate = false)
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            bool activate = dbManager.SetActiveInstanceState((long)LeaderboardId, (long)activeInstanceId, (int)state);

            if (dbUpdate && ActiveInstance != null && ActiveInstance.InstanceId != activeInstanceId)
                ActiveInstance.SaveEntries();

            ActiveInstance = GetInstance(activeInstanceId);
            return activate;
        }

        public void AddInstance(DBLeaderboardInstance dbInstance, bool loadEntries)
        {
            lock (_lock)
            {
                var instance = GetInstance((ulong)dbInstance.InstanceId);
                if (instance != null) return;

                instance = new(this, dbInstance);

                if (Prototype.IsMetaLeaderboard)
                    instance.InitMetaLeaderboardEntries(Prototype.MetaLeaderboardEntries);

                Instances.Add(instance);

                if (loadEntries) 
                    instance.LoadEntries();
                else
                    instance.UpdateCachedTableData();

                if (Prototype.IsMetaLeaderboard)
                    instance.LoadMetaInstances();
            }
        }

        public LeaderboardInstance GetInstance(ulong instanceId)
        {
            return Instances.Find(instance => instance.InstanceId == instanceId);
        }

        public void OnScoreUpdate(in LeaderboardQueue queue)
        {
            if (IsActive) ActiveInstance.OnScoreUpdate(queue);
        }

        public void UpdateState(DateTime updateTime)
        {
            lock (_lock)
            {
                DBLeaderboardInstance newInstanceDb = null;
                LeaderboardInstance metaInstance = null;
                foreach (var instance in Instances)
                {
                    switch (instance.State)
                    {
                        case LeaderboardState.eLBS_Created:

                            if (instance.IsActive(updateTime))
                                instance.SetState(LeaderboardState.eLBS_Active);

                            break;

                        case LeaderboardState.eLBS_Active:

                            if (instance.IsExpired(updateTime))
                            {
                                instance.SetState(LeaderboardState.eLBS_Expired);

                                if (CanReset && newInstanceDb == null && IsActive)
                                {
                                    var nextActivationTime = CalcNextUtcActivationDate(instance.ActivationTime, updateTime);
                                    if (nextActivationTime == instance.ActivationTime) continue;

                                    newInstanceDb = new()
                                    {
                                        InstanceId = NextInstanceId(),
                                        LeaderboardId = (long)LeaderboardId,
                                        State = LeaderboardState.eLBS_Created,
                                        Visible = instance.Visible
                                    };

                                    newInstanceDb.SetActivationDateTime(nextActivationTime);

                                    if (Prototype.IsMetaLeaderboard)
                                        metaInstance = instance;
                                }
                            }
                            else
                            {
                                if (ActiveInstance != instance)
                                {
                                    if (instance.InstanceId > ActiveInstance.InstanceId)
                                    {
                                        SetActiveInstance(instance.InstanceId, LeaderboardState.eLBS_Active, true);
                                    }
                                    else
                                    {
                                        instance.UpdateDBState(LeaderboardState.eLBS_Rewarded);
                                        instance.SetState(LeaderboardState.eLBS_Rewarded);
                                        break;
                                    }
                                }

                                instance.AutoSave();
                            }

                            break;

                        case LeaderboardState.eLBS_Expired:

                            if (CanReset)
                                instance.SetState(LeaderboardState.eLBS_Reward);

                            break;

                        case LeaderboardState.eLBS_Reward:

                            if (instance.SetState(LeaderboardState.eLBS_RewardsPending))
                                if (instance.GiveRewards())
                                {
                                    instance.UpdateDBState(LeaderboardState.eLBS_Rewarded);
                                    instance.SetState(LeaderboardState.eLBS_Rewarded);
                                }

                            break;
                    }
                }

                if (newInstanceDb != null)
                    AddNewInstance(newInstanceDb, metaInstance);
            }
        }

        private long NextInstanceId()
        {
            return (long)Instances.Max(i => i.InstanceId) + 1;
        }

        private void AddNewInstance(DBLeaderboardInstance dbInstance, LeaderboardInstance metaInstance)
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            dbManager.SetInstance(dbInstance);

            // add new MetaInstances
            metaInstance?.AddMetaInstances(dbInstance.InstanceId);

            AddInstance(dbInstance, true);
            OnStateChange((ulong)dbInstance.InstanceId, dbInstance.State);
            SetActiveInstance((ulong)dbInstance.InstanceId, dbInstance.State, true);
        }

        public DateTime CalcNextUtcActivationDate(DateTime activationTime, DateTime currentTime)
        {
            currentTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, 
                currentTime.Hour, currentTime.Minute, 0, currentTime.Kind);

            var nextReset = Scheduler.GetNextUtcResetDatetime(activationTime);

            if (nextReset < currentTime)
            {
                nextReset = new DateTime(
                    currentTime.Year, currentTime.Month, currentTime.Day,
                    nextReset.Hour, nextReset.Minute, nextReset.Second, DateTimeKind.Utc);

                if (nextReset < currentTime)
                    nextReset = Scheduler.GetNextUtcResetDatetime(nextReset);
            }

            var nextResetDay = new DateTime(nextReset.Year, nextReset.Month, nextReset.Day, 0, 0, 0, DateTimeKind.Utc);

            var nextActivation = Scheduler.GetNextActivationDate(currentTime);
            if (nextActivation.HasValue == false)
                return activationTime;
            else
                return nextActivation > nextResetDay ? nextActivation.Value : nextReset;
        }

        public void OnStateChange(ulong instanceId, LeaderboardState state)
        {
            var instance = GetInstance(instanceId);
            if (instance == null) return;

            var instanceInfo = instance.ToInstanceInfo();
            LeaderboardGameDatabase.Instance.OnLeaderboardStateChange(instanceInfo, state);
        }

        public void GetInstancesInfo(List<LeaderboardInstanceInfo> instancesInfo)
        {
            var maxInstances = Prototype.MaxArchivedInstances;
            foreach(var instance in Instances)
            {
                instancesInfo.Add(instance.ToInstanceInfo());
                if (--maxInstances < 0) break;
            }
        }
    }
}
