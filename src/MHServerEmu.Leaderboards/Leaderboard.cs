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

        public Leaderboard(LeaderboardPrototype proto, DBLeaderboard dBLeaderboard)
        {
            LeaderboardId = (PrototypeGuid)dBLeaderboard.LeaderboardId;
            Prototype = proto;

            var dbManager = LeaderboardDatabase.Instance.DBManager;
            var instanceList = dbManager.GetInstances(dBLeaderboard.LeaderboardId, proto.MaxArchivedInstances);
            foreach (var dbInstance in instanceList)
                AddInstance(dbInstance, true);

            if (dBLeaderboard.ActiveInstanceId != 0)
                SetActiveInstance((ulong)dBLeaderboard.ActiveInstanceId, LeaderboardState.eLBS_Active);
        }

        public static ulong GenInstanceId(PrototypeGuid leaderboardId)
        {
            return ((ulong)leaderboardId & 0xFFFFFFFF00000000UL) | 1UL;
        }

        public bool SetActiveInstance(ulong activeInstanceId, LeaderboardState state, bool dbUpdate = false)
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            bool ativate = dbManager.SetActiveInstanceState((long)LeaderboardId, (long)activeInstanceId, (int)state);

            if (dbUpdate && ActiveInstance != null && ActiveInstance.InstanceId != activeInstanceId)
                ActiveInstance.SaveEntries();

            ActiveInstance = GetInstance(activeInstanceId);
            return ativate;
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

                if (loadEntries) instance.LoadEntries();

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

                                if (CanReset && newInstanceDb == null)
                                {
                                    var nextActivationTime = CalcNextActivationDate(instance.ActivationTime);
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
                                    instance.SetState(LeaderboardState.eLBS_Rewarded);

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
            OnChangedState((ulong)dbInstance.InstanceId, dbInstance.State);
            SetActiveInstance((ulong)dbInstance.InstanceId, dbInstance.State, true);
        }

        private DateTime CalcNextActivationDate(DateTime activationTime)
        {
            return Prototype.ResetFrequency switch
            {
                LeaderboardResetFrequency.Every10minutes => activationTime.AddMinutes(10),
                LeaderboardResetFrequency.Every15minutes => activationTime.AddMinutes(15),
                LeaderboardResetFrequency.Every30minutes => activationTime.AddMinutes(30),
                LeaderboardResetFrequency.Every1hour => activationTime.AddHours(1),
                LeaderboardResetFrequency.Every2hours => activationTime.AddHours(2),
                LeaderboardResetFrequency.Every3hours => activationTime.AddHours(3),
                LeaderboardResetFrequency.Every4hours => activationTime.AddHours(4),
                LeaderboardResetFrequency.Every8hours => activationTime.AddHours(8),
                LeaderboardResetFrequency.Every12hours => activationTime.AddHours(12),
                LeaderboardResetFrequency.Daily => activationTime.AddDays(1),
                LeaderboardResetFrequency.Weekly => activationTime.AddDays(7),
                LeaderboardResetFrequency.Monthly => activationTime.AddMonths(1),
                _ => activationTime,
            };
        }

        public void OnChangedState(ulong instanceId, LeaderboardState state)
        {
            // TODO Update LeaderboardGameDatabase ?
        }
    }
}
