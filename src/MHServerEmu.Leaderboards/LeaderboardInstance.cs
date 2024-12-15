using Gazillion;
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
            lock (_lock)
            {
                return _cachedTableData;
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
