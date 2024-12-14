using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Leaderboards
{
    public class Leaderboard
    {
        public PrototypeGuid Guid;
        public LeaderboardPrototype Prototype;
        public List<LeaderboardInstance> Instances;
        public ulong LeaderboardId { get => (ulong)Guid; }
        public bool IsActive { get; set; }

        private Queue<LeaderboardQueue> _updateQueue = new();

        public LeaderboardInstance GetInstance(ulong instanceId)
        {
            return Instances.Find(instance => instance.InstanceId == instanceId);
        }

        // TODO UpdateProcess

        public void OnQueueUpdate(in LeaderboardQueue queue)
        {
            if (IsActive) _updateQueue.Enqueue(queue);
        }
    }

    public struct LeaderboardQueue
    {
        public ulong GameId;
        public ulong AvatarId;
        public long RuleId;
        public int Count;

        public LeaderboardQueue(in LeaderboardGuidKey key, int count)
        {
            GameId = key.PlayerGuid;
            AvatarId = (ulong)key.AvatarGuid;
            RuleId = key.RuleGuid;
            Count = count;
        }
    }

    public class LeaderboardInstance
    {
        private Leaderboard _leaderboard;
        private LeaderboardTableData _cachedTableData;

        public ulong LeaderboardId { get => _leaderboard.LeaderboardId; }
        public LeaderboardPrototype LeaderboardPrototype { get => _leaderboard.Prototype; }
        public ulong InstanceId { get; set; }
        public LeaderboardState State { get; set; }
        public TimeSpan ActivationTime { get; set; }
        public TimeSpan ExpirationTime { get; set; }
        public bool Visible { get; set; }

        /// <summary>
        /// Returns <see cref="LeaderboardInstanceData"/> from <see cref="LeaderboardInstance"/>.
        /// </summary>
        public LeaderboardInstanceData ToInstanceData()
        {
            return LeaderboardInstanceData.CreateBuilder()
                    .SetInstanceId(InstanceId)
                    .SetState(State)
                    .SetActivationTimestamp((long)ActivationTime.TotalSeconds)
                    .SetExpirationTimestamp((long)ExpirationTime.TotalSeconds)
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
                    .SetActivationTimestampUtc((long)ActivationTime.TotalSeconds)
                    .SetExpirationTimestampUtc((long)ExpirationTime.TotalSeconds)
                    .SetVisible(Visible)
                    .Build();
        }

        public LeaderboardEntry GetEntry(ulong guid, ulong avatarId)
        {
            throw new NotImplementedException();
        }

        public ulong GetLeaderboardEntryId(ulong guid)
        {
            throw new NotImplementedException();
        }

        public LeaderboardPercentile GetPercentileBucket(LeaderboardEntry entry)
        {
            throw new NotImplementedException();
        }

        public LeaderboardTableData GetTableData()
        {
            // TODO lock 
            return _cachedTableData;
        }

        public void UpdateCachedTableData()
        {
            // TODO lock 
            var tableDataBuilder = LeaderboardTableData.CreateBuilder()
                .SetInfo(ToMetadata());

            foreach (LeaderboardEntry entry in GetEntries(LeaderboardPrototype.DepthOfStandings))
                tableDataBuilder.AddEntries(entry.ToProtobuf());

            _cachedTableData = tableDataBuilder.Build();
        }

        private IEnumerable<LeaderboardEntry> GetEntries(int depthOfStandings)
        {
            throw new NotImplementedException();
        }
    }

    public class LeaderboardEntry
    {
        public uint GameId;
        public string Name;
        public LocaleStringId NameId;
        public uint Score;

        public Gazillion.LeaderboardEntry ToProtobuf()
        {
            var entryBuilder = Gazillion.LeaderboardEntry.CreateBuilder()
                .SetGameId(GameId)
                .SetName(Name)
                .SetScore(Score);

            if (NameId != LocaleStringId.Blank)
                entryBuilder.SetNameId((ulong)NameId);

            return entryBuilder.Build();
        }
    }
}
