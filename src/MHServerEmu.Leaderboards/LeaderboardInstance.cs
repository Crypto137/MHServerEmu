using Gazillion;
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

        public void LoadMetaInstances()
        {
            var dbManager = LeaderboardDatabase.Instance.DBManager;
            foreach (var dbMetaInstance in dbManager.GetMetaInstances((long)LeaderboardId, (long)InstanceId))
                SetMetaInstance((PrototypeGuid)dbMetaInstance.MetaLeaderboardId, (ulong)dbMetaInstance.MetaInstanceId);
        }

        private void SetMetaInstance(PrototypeGuid metaLeaderboardId, ulong metaInstanceId)
        {
            lock (_lock)
            {
                var leaderboard = LeaderboardDatabase.Instance.GetLeaderboard(metaLeaderboardId);
                if (leaderboard == null) return;
                var instance = leaderboard.GetInstance(InstanceId);
                if (instance == null) return;
                var metaEntry = _metaLeaderboardEntries.Find(meta => meta.MetaLeaderboardId == metaLeaderboardId);
                if (metaEntry == null) return;
                metaEntry.MetaInstance = instance;
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
                var dbManager = LeaderboardDatabase.Instance.DBManager;
                foreach (var dbEntry in dbManager.GetEntries((long)InstanceId, leaderboardProto.RankingRule == LeaderboardRankingRule.Ascending))
                {
                    LeaderboardEntry entry = new(dbEntry);
                    if (leaderboardProto.Type == LeaderboardType.MetaLeaderboard)
                    {
                        var dataRef = GameDatabase.GetDataRefByPrototypeGuid(entry.GameId);
                        var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                        entry.NameId = proto != null ? proto.Name : LocaleStringId.Blank;
                    }
                    else
                    {
                        entry.Name = LeaderboardDatabase.Instance.GetPlayerNameById(entry.GameId);
                    }

                    Entries.Add(entry);
                    _entryMap[entry.GameId] = entry;
                }

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

        public void OnUpdate(in LeaderboardQueue queue)
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
