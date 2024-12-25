using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardInstance
    {
        private Leaderboard _leaderboard; 
        private readonly object _lock = new object();
        private LeaderboardTableData _cachedTableData;

        public PrototypeGuid LeaderboardId { get => _leaderboard.LeaderboardId; }
        public LeaderboardPrototype LeaderboardPrototype { get => _leaderboard.Prototype; }
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public DateTime ActivationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool Visible { get; set; }
        public List<LeaderboardEntry> Entries { get; }

        private Dictionary<PrototypeGuid, LeaderboardEntry> _entryMap = new();
        private Dictionary<PrototypeGuid, PrototypeGuid> _entryGuidMap;
        private List<(LeaderboardPercentile Percentile, ulong Score)> _percentileBuckets;
        private List<MetaLeaderboardEntry> _metaLeaderboardEntries;
        private bool _sorted;
        private DateTime _lastSaveTime;

        public LeaderboardInstance(Leaderboard leaderboard, DBLeaderboardInstance dbInstance)
        {
            _leaderboard = leaderboard;
            InstanceId = (ulong)dbInstance.InstanceId;
            State = dbInstance.State;
            ActivationTime = dbInstance.GetActivationDateTime();
            ExpirationTime = CalcExpirationTime();
            Visible = dbInstance.Visible;
            Entries = new();

            InitPercentileBuckets();
        }

        private DateTime CalcExpirationTime()
        {
            return LeaderboardPrototype.Duration switch
            {
                LeaderboardDurationType._10minutes => ActivationTime.AddMinutes(10),
                LeaderboardDurationType._15minutes => ActivationTime.AddMinutes(15),
                LeaderboardDurationType._30minutes => ActivationTime.AddMinutes(30),
                LeaderboardDurationType._1hour => ActivationTime.AddHours(1),
                LeaderboardDurationType._2hours => ActivationTime.AddHours(2),
                LeaderboardDurationType._3hours => ActivationTime.AddHours(3),
                LeaderboardDurationType._4hours => ActivationTime.AddHours(4),
                LeaderboardDurationType._8hours => ActivationTime.AddHours(8),
                LeaderboardDurationType._12hours => ActivationTime.AddHours(12),
                LeaderboardDurationType.Day => ActivationTime.AddDays(1),
                LeaderboardDurationType.Week => ActivationTime.AddDays(7),
                LeaderboardDurationType.Month => ActivationTime.AddMonths(1),
                _ => ActivationTime,
            };
        }

        /// <summary>
        /// Returns <see cref="LeaderboardInstanceData"/> from <see cref="LeaderboardInstance"/>.
        /// </summary>
        public LeaderboardInstanceData ToInstanceData()
        {
            return LeaderboardInstanceData.CreateBuilder()
                    .SetInstanceId(InstanceId)
                    .SetState(State)
                    .SetActivationTimestamp((long)Clock.DateTimeToUnixTime(ActivationTime).TotalSeconds)
                    .SetExpirationTimestamp((long)Clock.DateTimeToUnixTime(ExpirationTime).TotalSeconds)
                    .SetVisible(Visible)
                    .Build();
        }

        /// <summary>
        /// Returns <see cref="LeaderboardMetadata"/> from <see cref="LeaderboardInstance"/>.
        /// </summary>
        public LeaderboardMetadata ToMetadata()
        {
            return LeaderboardMetadata.CreateBuilder()
                    .SetLeaderboardId((ulong)LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetState(State)
                    .SetActivationTimestampUtc((long)Clock.DateTimeToUnixTime(ActivationTime).TotalSeconds)
                    .SetExpirationTimestampUtc((long)Clock.DateTimeToUnixTime(ExpirationTime).TotalSeconds)
                    .SetVisible(Visible)
                    .Build();
        }

        /// <summary>
        /// Returns <see cref="LeaderboardInstanceInfo"/> from <see cref="LeaderboardInstance"/>.
        /// </summary>
        public LeaderboardInstanceInfo ToInstanceInfo()
        {
            return new LeaderboardInstanceInfo
            {
                LeaderboardId = LeaderboardId,
                InstanceId = InstanceId,
                State = State,
                ActivationTime = ActivationTime,
                ExpirationTime = ExpirationTime,
                Visible = Visible
            };
        }

        public LeaderboardEntry GetEntry(PrototypeGuid guid, ulong avatarId)
        {
            // avatarId don't used in active Leaderboards
            if (_entryMap.TryGetValue(guid, out var entry) == false) return null;
            return entry;
        }

        public PrototypeGuid GetMetaLeaderboardId(PrototypeGuid guid)
        {
            if (_entryGuidMap.TryGetValue(guid, out var metaLeaderboardId) == false)
                foreach (var entry in _metaLeaderboardEntries)
                    if (entry.MetaInstance != null && entry.MetaInstance.HasEntryGuid(guid))
                    {
                        metaLeaderboardId = entry.MetaLeaderboardId;
                        _entryGuidMap[guid] = metaLeaderboardId;
                        break;
                    }

            return metaLeaderboardId;
        }

        private bool HasEntryGuid(PrototypeGuid guid)
        {
            return _entryGuidMap.ContainsKey(guid);
        }

        private void InitPercentileBuckets()
        {
            _percentileBuckets = new();
            for (int i = 0; i < 10; i++)
                _percentileBuckets.Add(((LeaderboardPercentile)i, 0));
        }

        public LeaderboardPercentile GetPercentileBucket(LeaderboardEntry entry)
        {
            var proto = LeaderboardPrototype;
            if (proto == null) return LeaderboardPercentile.Over90Percent;
            var rankingRule = proto.RankingRule;

            foreach (var (percentile, score) in _percentileBuckets)
                if ((rankingRule == LeaderboardRankingRule.Ascending && entry.Score <= score)
                    || (rankingRule == LeaderboardRankingRule.Descending && entry.Score >= score))
                    return percentile;

            return LeaderboardPercentile.Over90Percent;
        }

        private void UpdatePercentileBuckets()
        {
            int totalEntries = Entries.Count;
            if (totalEntries == 0) return;

            for (int i = 0; i < 10; i++)
            {
                int thresholdIndex = (int)Math.Floor((i + 1) * 0.1 * totalEntries);
                if (thresholdIndex < totalEntries)
                    _percentileBuckets[i] = ((LeaderboardPercentile)i, Entries[thresholdIndex].Score);
            }
        }

        public LeaderboardTableData GetTableData()
        {
            lock (_lock)
            {
                return _cachedTableData;
            }
        }

        private void UpdateMetaInstances()
        {
            lock (_lock)
            { 
                foreach (var metaEntry in _metaLeaderboardEntries)
                {
                    CheckMetaInsance(metaEntry);
                    metaEntry.MetaInstance?.UpdateMetaScore(metaEntry.MetaLeaderboardId);
                }

                _sorted = false;
            }            
        }

        private void CheckMetaInsance(MetaLeaderboardEntry metaEntry)
        {
            if (metaEntry.MetaInstance == null)
            {
                if (metaEntry.MetaInstanceId == 0)
                {
                    var dbManager = LeaderboardDatabase.Instance.DBManager;
                    metaEntry.MetaInstanceId = (ulong)dbManager.GetMetaInstanceId((long)LeaderboardId, (long)InstanceId, (long)metaEntry.MetaLeaderboardId);
                }

                if (metaEntry.MetaInstanceId != 0)
                    SetMetaInstance(metaEntry.MetaLeaderboardId, metaEntry.MetaInstanceId);
            }
        }

        private void UpdateMetaScore(PrototypeGuid metaLeaderboardId)
        {
            lock (_lock)
            {
                ulong score = 0;
                foreach (var entry in Entries)
                    score += entry.Score;

                if (_entryMap.TryGetValue(metaLeaderboardId, out var updateEntry) == false)
                {
                    updateEntry = new(metaLeaderboardId);
                    _entryMap[metaLeaderboardId] = updateEntry;
                }

                updateEntry.Score = score;
                updateEntry.HighScore = score;
            }
        }

        public void LoadMetaInstances()
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            foreach (var dbMetaInstance in dbManager.GetMetaInstances((long)LeaderboardId, (long)InstanceId))
                SetMetaInstance((PrototypeGuid)dbMetaInstance.MetaLeaderboardId, (ulong)dbMetaInstance.MetaInstanceId);
        }

        public void AddMetaInstances(long instanceId)
        {            
            List<DBMetaInstance> metaInstances = new();
            foreach (var entry in _metaLeaderboardEntries)
                metaInstances.Add(
                    new DBMetaInstance
                    {
                        LeaderboardId = (long)LeaderboardId,
                        InstanceId = instanceId,
                        MetaInstanceId = (long)entry.MetaInstanceId + 1,
                        MetaLeaderboardId = (long)entry.MetaLeaderboardId 
                    });

            var dbManager = LeaderboardDatabase.Instance.DBManager;
            dbManager.SetMetaInstances(metaInstances);
        }

        private void SetMetaInstance(PrototypeGuid metaLeaderboardId, ulong metaInstanceId)
        {
            lock (_lock)
            {
                var leaderboard = LeaderboardDatabase.Instance.GetLeaderboard(metaLeaderboardId);
                if (leaderboard == null) return;
                var metaInstance = leaderboard.GetInstance(metaInstanceId);
                if (metaInstance == null) return;
                var metaEntry = _metaLeaderboardEntries.Find(meta => meta.MetaLeaderboardId == metaLeaderboardId);
                if (metaEntry == null) return;
                metaEntry.MetaInstance = metaInstance;
                metaEntry.MetaInstanceId = metaInstanceId;
            }
        }

        public void LoadEntries()
        {
            lock (_lock)
            {
                _entryMap.Clear();
                Entries.Clear();

                var leaderboardProto = LeaderboardPrototype;
                if (leaderboardProto == null) return;

                var dbManager = LeaderboardDatabase.Instance.DBManager;
                foreach (var dbEntry in dbManager.GetEntries((long)InstanceId, leaderboardProto.RankingRule == LeaderboardRankingRule.Ascending))
                {
                    LeaderboardEntry entry = new(dbEntry);
                    if (leaderboardProto.IsMetaLeaderboard)
                        entry.SetNameFromLeaderboardGuid(entry.GameId);
                    else
                        entry.Name = LeaderboardDatabase.Instance.GetPlayerNameById(entry.GameId);

                    Entries.Add(entry);
                    _entryMap[entry.GameId] = entry;
                }

                _sorted = true;
                _lastSaveTime = Clock.UtcNowPrecise;

                UpdatePercentileBuckets();
                UpdateCachedTableData();
            }
        }

        public void SortEntries()
        {
            lock (_lock)
            {
                var leaderboardProto = LeaderboardPrototype;
                if (leaderboardProto == null) return;

                if (leaderboardProto.IsMetaLeaderboard)
                    UpdateMetaInstances();

                if (_sorted) return;

                if (leaderboardProto.RankingRule == LeaderboardRankingRule.Ascending)
                    Entries.Sort((a, b) => a.Score.CompareTo(b.Score));
                else
                    Entries.Sort((a, b) => b.Score.CompareTo(a.Score));

                _sorted = true;

                UpdatePercentileBuckets();
                UpdateCachedTableData();
            }
        }

        public void SaveEntries(bool forceUpdate = false)
        {
            lock (_lock)
            {
                List<DBLeaderboardEntry> dbEntries = new();
                foreach (var entry in Entries)
                    if (forceUpdate || entry.NeedUpdate)
                        dbEntries.Add(entry.ToDbEntry(InstanceId));

                var dbManager = LeaderboardDatabase.Instance.DBManager;
                dbManager.SetEntries(dbEntries);

                _lastSaveTime = Clock.UtcNowPrecise;
            }
        }

        public void UpdateCachedTableData()
        {
            var tableDataBuilder = LeaderboardTableData.CreateBuilder()
                .SetInfo(ToMetadata());

            lock (_lock)
            {
                int depthOfStandings = LeaderboardPrototype.DepthOfStandings;
                foreach (var entry in Entries)
                {
                    if (depthOfStandings-- == 0) break;
                    tableDataBuilder.AddEntries(entry.ToProtobuf());
                }
                _cachedTableData = tableDataBuilder.Build();
            }
        }

        public void OnScoreUpdate(in LeaderboardQueue queue)
        {
            lock (_lock)
            {
                var gameId = queue.GameId;
                if (_entryMap.TryGetValue(gameId, out var entry) == false)
                {
                    entry = new(queue);
                    Entries.Add(entry);
                    _entryMap.Add(gameId, entry);
                }

                entry.UpdateScore(queue, LeaderboardPrototype);

                _sorted = false;
            }
        }

        public void InitMetaLeaderboardEntries(MetaLeaderboardEntryPrototype[] metaLeaderboardEntries)
        {
            if (metaLeaderboardEntries.IsNullOrEmpty()) return;

            _metaLeaderboardEntries = new();
            _entryGuidMap = new();
            foreach (var entryProto in metaLeaderboardEntries)
            {
                var metaLeaderboardId = GameDatabase.GetPrototypeGuid(entryProto.Leaderboard);
                if (metaLeaderboardId == PrototypeGuid.Invalid) continue;
                _metaLeaderboardEntries.Add(new(metaLeaderboardId, entryProto.Rewards));
            }
        }

        public bool GiveRewards()
        {
            lock (_lock) 
            {
                if (Entries.Count == 0) return true;

                List<DBRewardEntry> rewardsList = new();

                var rewards = LeaderboardPrototype.Rewards;
                if (rewards.HasValue())
                    GetRewards(rewards, rewardsList);

                if (LeaderboardPrototype.IsMetaLeaderboard)
                    GetMetaRewards(rewardsList);

                var dbManager = LeaderboardDatabase.Instance.DBManager;
                dbManager.SetRewards(rewardsList);
            }
            return true;
        }

        private void GetMetaRewards(List<DBRewardEntry> rewardsList)
        {
            ulong prevScore = 0;
            int prevRank = 0;
            int entryIndex = 0;

            foreach (var entry in Entries)
            {
                int rank = entryIndex + 1;

                if (entry.Score == prevScore)
                    rank = prevRank;

                var metaEntry = _metaLeaderboardEntries.Find(meta => meta.MetaLeaderboardId == entry.GameId);
                if (metaEntry == null || metaEntry.MetaInstance == null || metaEntry.Rewards.IsNullOrEmpty()) 
                    continue;

                foreach (var rewardProto in metaEntry.Rewards)
                    if (EvaluateReward(rewardProto, entry, rank))
                    {
                        var rewardId = GameDatabase.GetPrototypeGuid(rewardProto.RewardItem);

                        foreach (var entryInst in metaEntry.MetaInstance.Entries)
                            rewardsList.Add(new DBRewardEntry(
                                (long)LeaderboardId, (long)InstanceId, 
                                (long)rewardId, (long)entryInst.GameId, rank));

                        prevScore = entry.Score;
                        prevRank = rank;

                        break;
                    }
                
                entryIndex++;
            }
        }

        private void GetRewards(LeaderboardRewardEntryPrototype[] rewards, List<DBRewardEntry> rewardsList)
        {
            int count = Entries.Count;

            ulong prevScore = 0;
            int prevRank = 0;
            int entryIndex = 0;

            foreach (var rewardProto in rewards)
            {
                while (entryIndex < count)
                {
                    var entry = Entries[entryIndex];
                    int rank = entryIndex + 1;

                    if (entry.Score == prevScore)
                        rank = prevRank;

                    if (EvaluateReward(rewardProto, entry, rank) == false) break;

                    var rewardId = GameDatabase.GetPrototypeGuid(rewardProto.RewardItem);
                    rewardsList.Add(new DBRewardEntry(
                        (long)LeaderboardId, (long)InstanceId, 
                        (long)rewardId, (long)entry.GameId, rank));

                    prevScore = entry.Score;
                    prevRank = rank;

                    entryIndex++;
                }
            }
        }

        private bool EvaluateReward(LeaderboardRewardEntryPrototype rewardProto, LeaderboardEntry entry, int position)
        {
            if (rewardProto is LeaderboardRewardEntryPercentilePrototype percentileProto)
                return GetPercentileBucket(entry) <= percentileProto.PercentileBucket;

            if (rewardProto is LeaderboardRewardEntryPositionPrototype positionProto)            
                return position <= positionProto.Position;

            if (rewardProto is LeaderboardRewardEntryScorePrototype scoreProto)
            {
                if (LeaderboardPrototype.RankingRule == LeaderboardRankingRule.Ascending)
                    return (int)entry.Score <= scoreProto.Score;
                else
                    return (int)entry.Score >= scoreProto.Score;
            }            

            return false;
        }

        public bool IsExpired(DateTime currentTime)
        {
            return ExpirationTime != ActivationTime && currentTime >= ExpirationTime;
        }

        public bool IsActive(DateTime currentTime)
        {
            return currentTime >= ActivationTime;
        }

        public bool SetState(LeaderboardState state)
        {
            bool changed = false;

            lock (_lock)
            {
                switch (state)
                {
                    case LeaderboardState.eLBS_Created:

                        changed = _leaderboard.SetActiveInstance(InstanceId, state, true);
                        break;

                    case LeaderboardState.eLBS_Active:

                        if (State == LeaderboardState.eLBS_Created)
                            changed = _leaderboard.SetActiveInstance(InstanceId, state, true);
                        break;

                    case LeaderboardState.eLBS_Expired:

                        if (State == LeaderboardState.eLBS_Active)
                        {
                            SortEntries();
                            SaveEntries(true);
                        }

                        changed = true;
                        break;

                    case LeaderboardState.eLBS_Reward:

                        changed = true;
                        break;

                    case LeaderboardState.eLBS_RewardsPending:

                        changed = State == LeaderboardState.eLBS_Reward;
                        break;

                    case LeaderboardState.eLBS_Rewarded:

                        changed = State == LeaderboardState.eLBS_RewardsPending;
                        break;
                }

                if (changed)
                {
                    State = state;
                    _leaderboard.OnStateChange(InstanceId, state);
                }
            }

            return changed;
        }

        public void UpdateDBState(LeaderboardState state)
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            dbManager.SetInstanceState((long)InstanceId, (int)state);
        }

        public void AutoSave()
        {
            if (_sorted == false) SortEntries();

            var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();
            int intervalMinutes = config.AutoSaveIntervalMinutes;
            if (_lastSaveTime.AddMinutes(intervalMinutes) < Clock.UtcNowPrecise)
                SaveEntries();
        }
    }

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
