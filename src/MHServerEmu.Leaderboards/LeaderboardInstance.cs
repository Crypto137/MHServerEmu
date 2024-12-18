using Gazillion;
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

        public ulong LeaderboardId { get => _leaderboard.LeaderboardId; }
        public LeaderboardPrototype LeaderboardPrototype { get => _leaderboard.Prototype; }
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public DateTime ActivationTime { get; set; }
        public DateTime ExpirationTime { get; set; }
        public bool Visible { get; set; }
        public List<LeaderboardEntry> Entries { get; }

        private Dictionary<PrototypeGuid, LeaderboardEntry> _entryMap = new();

        public LeaderboardInstance(Leaderboard leaderboard, DBLeaderboardInstance dbInstance)
        {
            _leaderboard = leaderboard;
            InstanceId = (ulong)dbInstance.InstanceId;
            State = dbInstance.State;
            ActivationTime = dbInstance.GetActivationDateTime();
            ExpirationTime = CalcExpirationTime();
            Visible = dbInstance.Visible;
            Entries = new();
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
                    .SetLeaderboardId(LeaderboardId)
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

        public PrototypeGuid GetLeaderboardEntryId(PrototypeGuid guid)
        {
            throw new NotImplementedException();
        }

        public LeaderboardPercentile GetPercentileBucket(LeaderboardEntry entry)
        {
            throw new NotImplementedException();
        }

        private void UpdatePercentileBuckets()
        {
            throw new NotImplementedException();
        }

        public LeaderboardTableData GetTableData()
        {
            lock (_lock)
            {
                return _cachedTableData;
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

        public void UpdateCachedTableData()
        {
            var tableDataBuilder = LeaderboardTableData.CreateBuilder()
                .SetInfo(ToMetadata());

            foreach (LeaderboardEntry entry in GetEntries(LeaderboardPrototype.DepthOfStandings))
                tableDataBuilder.AddEntries(entry.ToProtobuf());

            lock (_lock)
            {
                _cachedTableData = tableDataBuilder.Build();
            }
        }

        private IEnumerable<LeaderboardEntry> GetEntries(int depthOfStandings)
        {
            throw new NotImplementedException();
        }

        public void OnUpdate(LeaderboardQueue queue)
        {
            throw new NotImplementedException();
        }
    }
}
