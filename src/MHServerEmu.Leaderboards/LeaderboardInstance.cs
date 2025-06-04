using System.Text;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardInstance
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly int AutoSaveIntervalMinutes = ConfigManager.Instance.GetConfig<LeaderboardsConfig>().AutoSaveIntervalMinutes;

        private readonly object _lock = new();
        private readonly Leaderboard _leaderboard;

        private readonly Dictionary<ulong, LeaderboardEntry> _entryMap = new();
        private Dictionary<ulong, PrototypeGuid> _subLeaderboardParticipantMap;
        private List<(LeaderboardPercentile Percentile, ulong Score)> _percentileBuckets;
        private List<MetaLeaderboardEntry> _metaLeaderboardEntries;
        private bool _sorted;
        private DateTime _nextAutoSaveTime;

        private LeaderboardTableData _cachedTableData;

        public PrototypeGuid LeaderboardId { get => _leaderboard.LeaderboardId; }
        public LeaderboardPrototype LeaderboardPrototype { get => _leaderboard.Prototype; }
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public DateTime ActivationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool Visible { get; set; }
        public List<LeaderboardEntry> Entries { get; } = new();

        /// <summary>
        /// Constructs a new <see cref="LeaderboardInstance"/> for the provided <see cref="Leaderboard"/>.
        /// </summary>
        public LeaderboardInstance(Leaderboard leaderboard, DBLeaderboardInstance dbInstance)
        {
            _leaderboard = leaderboard;

            InstanceId = (ulong)dbInstance.InstanceId;
            State = dbInstance.State;

            if (dbInstance.ActivationDate == 0 && leaderboard.CanReset)
            {
                DateTime activationDate = _leaderboard.Scheduler.CalcNextUtcActivationDate();
                dbInstance.SetActivationDateTime(activationDate);
                var dbManager = LeaderboardDatabase.Instance.DBManager;
                dbManager.UpdateInstanceActivationDate(dbInstance);
            }

            ActivationTime = dbInstance.GetActivationDateTime();
            ExpirationTime = _leaderboard.Scheduler.CalcExpirationTime(ActivationTime);
            Visible = dbInstance.Visible;

            InitPercentileBuckets();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Instance {InstanceId}");
            sb.AppendLine($"Leaderboard Prototype: {LeaderboardPrototype.DataRef.GetNameFormatted()}");
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"Activation Time: {ActivationTime}");
            sb.AppendLine($"Expiration Time: {ExpirationTime}");
            sb.AppendLine($"Visible: {Visible}");
            return sb.ToString();
        }

        /// <summary>
        /// Builds <see cref="LeaderboardMetadata"/> for this <see cref="LeaderboardInstance"/>.
        /// </summary>
        public LeaderboardMetadata BuildMetadata()
        {
            return LeaderboardMetadata.CreateBuilder()
                .SetLeaderboardId((ulong)LeaderboardId)
                .SetInstanceId(InstanceId)
                .SetState(State)
                .SetActivationTimestampUtc(Clock.DateTimeToTimestamp(ActivationTime))
                .SetExpirationTimestampUtc(Clock.DateTimeToTimestamp(ExpirationTime))
                .SetVisible(Visible)
                .Build();
        }

        /// <summary>
        /// Builds <see cref="GameServiceProtocol.LeaderboardStateChange"/> for this <see cref="LeaderboardInstance"/>.
        /// </summary>
        public GameServiceProtocol.LeaderboardStateChange BuildLeaderboardStateChange(LeaderboardState? stateOverride = null)
        {
            return new((ulong)LeaderboardId,
                InstanceId,
                stateOverride != null ? stateOverride.Value : State,
                ActivationTime,
                ExpirationTime,
                Visible);
        }

        /// <summary>
        /// Returns the <see cref="LeaderboardEntry"/> for the specified participant. Returns <see langword="null"/> if not found.
        /// </summary>
        public LeaderboardEntry GetEntry(ulong participantId, ulong avatarId)
        {
            // avatarId isn't used in active Leaderboards
            if (_entryMap.TryGetValue(participantId, out LeaderboardEntry entry) == false)
                return null;

            return entry;
        }

        /// <summary>
        /// Returns the <see cref="PrototypeGuid"/> of the subleaderboard that contains the specified participant.
        /// </summary>
        public PrototypeGuid GetSubLeaderboardId(ulong participantId)
        {
            if (_subLeaderboardParticipantMap.TryGetValue(participantId, out PrototypeGuid subLeaderboardId) == false)
            {
                // Look for a subleaderboard that has the specified participant and cache it.
                foreach (MetaLeaderboardEntry entry in _metaLeaderboardEntries)
                {
                    if (entry.SubInstance != null && entry.SubInstance.HasParticipant(participantId))
                    {
                        subLeaderboardId = entry.SubLeaderboardId;
                        _subLeaderboardParticipantMap[participantId] = subLeaderboardId;
                        break;
                    }
                }
            }

            return subLeaderboardId;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="LeaderboardInstance"/> contains an entry for the specified participant.
        /// </summary>
        private bool HasParticipant(ulong participantId)
        {
            return _entryMap.ContainsKey(participantId);
        }

        /// <summary>
        /// Initializes <see cref="LeaderboardPercentile"/> buckets.
        /// </summary>
        private void InitPercentileBuckets()
        {
            _percentileBuckets = new();
            for (int i = 0; i < 10; i++)
                _percentileBuckets.Add(((LeaderboardPercentile)i, 0));
        }

        /// <summary>
        /// Returns the <see cref="LeaderboardPercentile"/> for the provided <see cref="LeaderboardEntry"/>.
        /// </summary>
        public LeaderboardPercentile GetPercentileBucket(LeaderboardEntry entry)
        {
            LeaderboardPrototype proto = LeaderboardPrototype;
            if (proto == null)
                return LeaderboardPercentile.Over90Percent;

            LeaderboardRankingRule rankingRule = proto.RankingRule;

            foreach (var (percentile, score) in _percentileBuckets)
                if ((rankingRule == LeaderboardRankingRule.Ascending && entry.Score <= score)
                    || (rankingRule == LeaderboardRankingRule.Descending && entry.Score >= score))
                    return percentile;

            return LeaderboardPercentile.Over90Percent;
        }

        /// <summary>
        /// Updates <see cref="LeaderboardPercentile"/> buckets based on the current total number of entries.
        /// </summary>
        private void UpdatePercentileBuckets()
        {
            int totalEntries = Entries.Count;
            if (totalEntries == 0)
                return;

            for (int i = 0; i < 10; i++)
            {
                int thresholdIndex = (int)Math.Floor((i + 1) * 0.1 * totalEntries);
                if (thresholdIndex < totalEntries)
                    _percentileBuckets[i] = ((LeaderboardPercentile)i, Entries[thresholdIndex].Score);
            }
        }

        /// <summary>
        /// Returns cached <see cref="LeaderboardTableData"/>.
        /// </summary>
        public LeaderboardTableData GetTableData()
        {
            lock (_lock)
            {
                return _cachedTableData;
            }
        }

        /// <summary>
        /// Updates the score of all subleaderboard instances.
        /// </summary>
        private void UpdateMetaScore()
        {
            lock (_lock)
            { 
                foreach (MetaLeaderboardEntry metaEntry in _metaLeaderboardEntries)
                {
                    RestoreSubInstanceReference(metaEntry);

                    PrototypeGuid subLeaderboardId = metaEntry.SubLeaderboardId;
                    LeaderboardInstance subInstance = metaEntry.SubInstance;
                    
                    // We need a subinstance here
                    if (subInstance == null)
                    {
                        Logger.Warn("UpdateMetaScore(): subInstance == null");
                        continue;
                    }

                    ulong score = 0;
                    foreach (LeaderboardEntry subEntry in subInstance.Entries)
                        score += subEntry.Score;

                    if (_entryMap.TryGetValue((ulong)subLeaderboardId, out LeaderboardEntry updateEntry) == false)
                    {
                        updateEntry = new(subLeaderboardId);
                        Entries.Add(updateEntry);
                        _entryMap[(ulong)subLeaderboardId] = updateEntry;
                    }

                    updateEntry.Score = score;
                    updateEntry.HighScore = score;
                }

                _sorted = false;
            }            
        }

        /// <summary>
        /// Restores the reference to the SubLeaderboard's <see cref="LeaderboardInstance"/> for the provided <see cref="MetaLeaderboardEntry"/>.
        /// </summary>
        private bool RestoreSubInstanceReference(MetaLeaderboardEntry metaEntry)
        {
            // No need to do anything if we already have a reference
            if (metaEntry.SubInstance != null)
                return true;

            // Query the subleaderboard's instance id from the database if we don't have one
            if (metaEntry.SubInstanceId == 0)
            {
                var dbManager = LeaderboardDatabase.Instance.DBManager;
                metaEntry.SubInstanceId = (ulong)dbManager.GetSubInstanceId((long)LeaderboardId, (long)InstanceId, (long)metaEntry.SubLeaderboardId);
            }

            if (metaEntry.SubInstanceId == 0)
                return Logger.WarnReturn(false, $"RestoreSubInstanceReference(): Failed to retrieve SubInstanceId for SubLeaderboard {metaEntry.SubLeaderboardId}");

            SetSubInstance(metaEntry.SubLeaderboardId, metaEntry.SubInstanceId);
            return true;
        }

        /// <summary>
        /// Loads <see cref="MetaLeaderboardEntry"/> data from the database.
        /// </summary>
        public void LoadMetaEntries()
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            foreach (DBMetaEntry dbMetaEntry in dbManager.GetMetaEntries((long)LeaderboardId, (long)InstanceId))
                SetSubInstance((PrototypeGuid)dbMetaEntry.SubLeaderboardId, (ulong)dbMetaEntry.SubInstanceId);
        }

        /// <summary>
        /// Writes new <see cref="DBMetaEntry"/> instances for the next instance of this metaleaderboard.
        /// </summary>
        public void AddNewMetaEntries(ulong instanceId)
        {
            // This assumes that meta leaderboards will always stay in sync with their subleaderboards, which is not ideal.
            // Because this is used only for the Civil War leaderboard it's probably fine, but it will have to change if we ever implement custom meta leaderboards.
            List<DBMetaEntry> metaEntries = new();
            foreach (MetaLeaderboardEntry entry in _metaLeaderboardEntries)
            {
                metaEntries.Add(
                    new DBMetaEntry
                    {
                        LeaderboardId = (long)LeaderboardId,
                        InstanceId = (long)instanceId,
                        SubInstanceId = (long)(entry.SubInstanceId + 1),
                        SubLeaderboardId = (long)entry.SubLeaderboardId 
                    });
            }

            var dbManager = LeaderboardDatabase.Instance.DBManager;
            dbManager.InsertMetaEntries(metaEntries);
        }

        /// <summary>
        /// Binds a SubLeaderboard instance to this MetaLeaderboard.
        /// </summary>
        private void SetSubInstance(PrototypeGuid subLeaderboardId, ulong subInstanceId)
        {
            lock (_lock)
            {
                Leaderboard leaderboard = LeaderboardDatabase.Instance.GetLeaderboard(subLeaderboardId);
                if (leaderboard == null)
                    return;
                
                LeaderboardInstance subInstance = leaderboard.GetInstance(subInstanceId, true);
                if (subInstance == null)
                    return;
                
                MetaLeaderboardEntry metaEntry = _metaLeaderboardEntries.Find(entry => entry.SubLeaderboardId == subLeaderboardId);
                if (metaEntry == null)
                    return;
                
                metaEntry.SubInstance = subInstance;
                metaEntry.SubInstanceId = subInstanceId;
            }
        }

        /// <summary>
        /// Loads <see cref="LeaderboardEntry">LeaderboardEntries</see> from the database and updates cache.
        /// </summary>
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
                        entry.SetNameFromLeaderboardGuid((PrototypeGuid)entry.ParticipantId);
                    else
                        entry.Name = LeaderboardDatabase.Instance.GetPlayerNameById(entry.ParticipantId);

                    Entries.Add(entry);
                    _entryMap[entry.ParticipantId] = entry;
                }

                _sorted = true;

                UpdatePercentileBuckets();
                UpdateCachedTableData();

                ScheduleNextAutoSave();
            }
        }

        /// <summary>
        /// Sorts <see cref="LeaderboardEntry">LeaderboardEntries</see> and updates cache.
        /// </summary>
        public void SortEntries()
        {
            lock (_lock)
            {
                LeaderboardPrototype leaderboardProto = LeaderboardPrototype;
                if (leaderboardProto == null) return;

                if (leaderboardProto.IsMetaLeaderboard)
                    UpdateMetaScore();

                if (_sorted)
                    return;

                if (leaderboardProto.RankingRule == LeaderboardRankingRule.Ascending)
                    Entries.Sort((a, b) => a.Score.CompareTo(b.Score));
                else
                    Entries.Sort((a, b) => b.Score.CompareTo(a.Score));

                _sorted = true;

                UpdatePercentileBuckets();
                UpdateCachedTableData();
            }
        }

        /// <summary>
        /// Saves <see cref="LeaderboardEntry">LeaderboardEntries</see> to the database.
        /// </summary>
        /// <param name="forceUpdate">Forces entries that haven't changed to be saved.</param>
        public void SaveEntries(bool forceUpdate = false)
        {
            List<DBLeaderboardEntry> dbEntries = ListPool<DBLeaderboardEntry>.Instance.Get();

            lock (_lock)
            {
                foreach (LeaderboardEntry entry in Entries)
                {
                    if (forceUpdate || entry.SaveRequired)
                    {
                        DBLeaderboardEntry dbEntry = entry.ToDbEntry(InstanceId);
                        dbEntries.Add(dbEntry);
                        entry.SaveRequired = false;
                    }
                }

                var dbManager = LeaderboardDatabase.Instance.DBManager;
                dbManager.UpdateOrInsertEntries(dbEntries);

                ScheduleNextAutoSave();
            }

            ListPool<DBLeaderboardEntry>.Instance.Return(dbEntries);
        }

        /// <summary>
        /// Updates cached <see cref="LeaderboardTableData"/>.
        /// </summary>
        public void UpdateCachedTableData()
        {
            var tableDataBuilder = LeaderboardTableData.CreateBuilder()
                .SetInfo(BuildMetadata());

            lock (_lock)
            {
                int depthOfStandings = LeaderboardPrototype.DepthOfStandings;
                foreach (LeaderboardEntry entry in Entries)
                {
                    if (depthOfStandings-- == 0)
                        break;

                    tableDataBuilder.AddEntries(entry.ToProtobuf());
                }
                _cachedTableData = tableDataBuilder.Build();
            }
        }

        /// <summary>
        /// Updates score using data from the provided <see cref="GameServiceProtocol.LeaderboardScoreUpdate"/>.
        /// </summary>
        public void OnScoreUpdate(ref GameServiceProtocol.LeaderboardScoreUpdate update)
        {
            lock (_lock)
            {
                ulong participantId = update.ParticipantId;
                if (_entryMap.TryGetValue(participantId, out LeaderboardEntry entry) == false)   
                {
                    entry = new(ref update);
                    Entries.Add(entry);
                    _entryMap.Add(participantId, entry);
                }

                entry.UpdateScore(ref update, LeaderboardPrototype);

                _sorted = false;
            }
        }

        /// <summary>
        /// Initializes <see cref="MetaLeaderboardEntry"/> instances.
        /// </summary>
        public void InitMetaLeaderboardEntries(MetaLeaderboardEntryPrototype[] metaLeaderboardEntries)
        {
            if (metaLeaderboardEntries.IsNullOrEmpty())
                return;

            _metaLeaderboardEntries = new();
            _subLeaderboardParticipantMap = new();

            foreach (MetaLeaderboardEntryPrototype entryProto in metaLeaderboardEntries)
            {
                PrototypeGuid subLeaderboardId = GameDatabase.GetPrototypeGuid(entryProto.Leaderboard);
                if (subLeaderboardId == PrototypeGuid.Invalid)
                    continue;

                _metaLeaderboardEntries.Add(new(subLeaderboardId, entryProto.Rewards));
            }
        }

        /// <summary>
        /// Distributes rewards to participants of this <see cref="LeaderboardInstance"/>.
        /// </summary>
        public bool GiveRewards()
        {
            lock (_lock) 
            {
                if (Entries.Count == 0)
                    return true;

                List<DBRewardEntry> rewardsList = ListPool<DBRewardEntry>.Instance.Get();

                LeaderboardRewardEntryPrototype[] rewards = LeaderboardPrototype.Rewards;
                if (rewards.HasValue())
                    GetRewards(rewards, rewardsList);

                if (LeaderboardPrototype.IsMetaLeaderboard)
                    GetMetaRewards(rewardsList);

                var dbManager = LeaderboardDatabase.Instance.DBManager;
                dbManager.InsertRewards(rewardsList);

                ListPool<DBRewardEntry>.Instance.Return(rewardsList);
            }

            return true;
        }

        /// <summary>
        /// Determines rewards for participants of this MetaLeaderboard and adds them to the provided <see cref="List{T}"/>.
        /// </summary>
        private void GetMetaRewards(List<DBRewardEntry> rewardsList)
        {
            ulong prevScore = 0;
            int prevRank = 0;
            int entryIndex = 0;

            foreach (LeaderboardEntry entry in Entries)
            {
                int rank = entryIndex + 1;

                if (entry.Score == prevScore)
                    rank = prevRank;

                MetaLeaderboardEntry metaEntry = _metaLeaderboardEntries.Find(metaEntry => (ulong)metaEntry.SubLeaderboardId == entry.ParticipantId);
                if (metaEntry == null || metaEntry.SubInstance == null || metaEntry.Rewards.IsNullOrEmpty()) 
                    continue;

                foreach (LeaderboardRewardEntryPrototype rewardProto in metaEntry.Rewards)
                {
                    if (EvaluateReward(rewardProto, entry, rank))
                    {
                        PrototypeGuid rewardId = GameDatabase.GetPrototypeGuid(rewardProto.RewardItem);

                        foreach (LeaderboardEntry subEntry in metaEntry.SubInstance.Entries)
                            rewardsList.Add(new DBRewardEntry(
                                (long)LeaderboardId, (long)InstanceId,
                                (long)rewardId, (long)subEntry.ParticipantId, rank));

                        prevScore = entry.Score;
                        prevRank = rank;

                        break;
                    }
                }
                
                entryIndex++;
            }
        }

        /// <summary>
        /// Determines rewards for participants of this <see cref="LeaderboardInstance"/> and adds them to the provided <see cref="List{T}"/>.
        /// </summary>
        private void GetRewards(LeaderboardRewardEntryPrototype[] rewards, List<DBRewardEntry> rewardsList)
        {
            int count = Entries.Count;

            ulong prevScore = 0;
            int prevRank = 0;
            int entryIndex = 0;

            foreach (LeaderboardRewardEntryPrototype rewardProto in rewards)
            {
                while (entryIndex < count)
                {
                    LeaderboardEntry entry = Entries[entryIndex];
                    int rank = entryIndex + 1;

                    if (entry.Score == prevScore)
                        rank = prevRank;

                    if (EvaluateReward(rewardProto, entry, rank) == false)
                        break;

                    PrototypeGuid rewardId = GameDatabase.GetPrototypeGuid(rewardProto.RewardItem);
                    rewardsList.Add(new DBRewardEntry(
                        (long)LeaderboardId, (long)InstanceId, 
                        (long)rewardId, (long)entry.ParticipantId, rank));

                    prevScore = entry.Score;
                    prevRank = rank;

                    entryIndex++;
                }
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="LeaderboardEntry"/> is eligible for the specified <see cref="LeaderboardRewardEntryPrototype"/>.
        /// </summary>
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

        /// <summary>
        /// Return <see langword="true"/> if this <see cref="LeaderboardInstance"/> has expired.
        /// </summary>
        public bool IsExpired(DateTime currentTime)
        {
            return ExpirationTime != ActivationTime && currentTime >= ExpirationTime;
        }

        /// <summary>
        /// Return <see langword="true"/> if this <see cref="LeaderboardInstance"/> is active.
        /// </summary>
        public bool IsActive(DateTime currentTime)
        {
            return currentTime >= ActivationTime || _leaderboard.CanReset == false;
        }

        /// <summary>
        /// Updates the <see cref="LeaderboardState"/> of this <see cref="LeaderboardInstance"/>.
        /// Returns <see langword="true"/> if the state changed.
        /// </summary>
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
                    Logger.Info($"SetState(): {LeaderboardPrototype.DataRef.GetNameFormatted()} {InstanceId} [{State}] => [{state}]");
                    State = state;
                    _leaderboard.OnStateChange(InstanceId, state);
                }
            }

            return changed;
        }

        /// <summary>
        /// Writes the new <see cref="LeaderboardState"/> to the database.
        /// </summary>
        public void UpdateDBState(LeaderboardState state)
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            dbManager.UpdateInstanceState((long)InstanceId, (int)state);
        }

        /// <summary>
        /// Sorts this <see cref="LeaderboardInstance"/> and saves it to the database if enough time has passed.
        /// </summary>
        public void Update()
        {
            SortEntries();

            if (Clock.UtcNowPrecise >= _nextAutoSaveTime)
                SaveEntries();                
        }

        /// <summary>
        /// Schedules the next time entries for this <see cref="LeaderboardInstance"/> will be saved to the database.
        /// </summary>
        private void ScheduleNextAutoSave()
        {
            _nextAutoSaveTime = Clock.UtcNowPrecise.AddMinutes(AutoSaveIntervalMinutes);
        }
    }
}
