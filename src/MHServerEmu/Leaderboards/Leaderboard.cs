using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{
    public class Leaderboard
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private const ulong UpdateTimeIntervalMS = 60 * 1000;   // Every minute

        private readonly List<LeaderboardEntry> _entryList = new();

        public PrototypeGuid LeaderboardId { get; }
        public ulong InstanceId { get; }
        public LeaderboardState State { get; set; }
        public TimeSpan ActivationTimestampUtc { get; set; }
        public TimeSpan ExpirationTimestampUtc { get; set; }
        public bool Visible { get; set; }

        /// <summary>
        /// Constructs a new <see cref="Leaderboard"/> instance.
        /// </summary>
        public Leaderboard(PrototypeGuid leaderboardId, ulong instanceId, LeaderboardState state,
            TimeSpan activationTimestampUtc, TimeSpan expirationTimestampUtc, bool visible)
        {
            LeaderboardId = leaderboardId;
            InstanceId = instanceId;
            State = state;
            ActivationTimestampUtc = activationTimestampUtc;
            ExpirationTimestampUtc = expirationTimestampUtc;
            Visible = visible;

            if (LeaderboardId != (PrototypeGuid)17890326285325567482)
            {
                // Default dummy leaderboard data
                _entryList = new()
                {
                    LeaderboardEntry.CreateBuilder().SetName("DavidBrevik").SetGameId(1).SetScore(9001).Build(),
                    LeaderboardEntry.CreateBuilder().SetName("Doomsaw").SetGameId(2).SetScore(9001).Build(),
                    LeaderboardEntry.CreateBuilder().SetName("MHServerEmu User").SetGameId(3).SetScore(9000).Build(),
                    LeaderboardEntry.CreateBuilder().SetName("RogueServerEnjoyer").SetGameId(4).SetScore(7777).Build(),
                    LeaderboardEntry.CreateBuilder().SetName("WhiteQueenXOXO").SetGameId(5).SetScore(6666).Build()
                };

                for (ulong i = 6; i < 51; i++)
                    _entryList.Add(LeaderboardEntry.CreateBuilder().SetName($"Player {i}").SetGameId(i).SetScore(6000 - i * 100).Build());
            }
            else
            {
                // Tournament: Civil War dummy data
                _entryList = new()
                {
                    LeaderboardEntry.CreateBuilder().SetName("Anti-Registration").SetGameId(1).SetScore(1000).Build(),
                    LeaderboardEntry.CreateBuilder().SetName("Pro-Registration").SetGameId(2).SetScore(1000).Build()
                };
            }
        }

        /// <summary>
        /// Returns <see cref="LeaderboardInstanceData"/> for this <see cref="Leaderboard"/>.
        /// </summary>
        public LeaderboardInstanceData GetInstanceData()
        {
            return LeaderboardInstanceData.CreateBuilder()
                .SetInstanceId(InstanceId)
                .SetState(State)
                .SetActivationTimestamp((long)ActivationTimestampUtc.TotalSeconds)
                .SetExpirationTimestamp((long)ExpirationTimestampUtc.TotalSeconds)
                .SetVisible(Visible)
                .Build();
        }

        /// <summary>
        /// Generates a <see cref="LeaderboardReport"/> for this <see cref="Leaderboard"/>.
        /// </summary>
        public LeaderboardReport GetReport(NetMessageLeaderboardRequest request)
        {
            var report = LeaderboardReport.CreateBuilder()
                .SetLeaderboardId((ulong)LeaderboardId)
                .SetInstanceId(InstanceId)
                .SetNextUpdateTimeIntervalMS(UpdateTimeIntervalMS);

            if (request.HasPlayerScoreQuery)
            {
                // Set cool score data to make players feel good about themselves
                var scoreData = LeaderboardScoreData.CreateBuilder()
                    .SetLeaderboardId((ulong)LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetPlayerId(request.PlayerScoreQuery.PlayerId)
                    .SetScore(9000)
                    .SetPercentileBucket((uint)LeaderboardPercentile.Within10Percent);

                // Add avatar id if needed
                if (request.PlayerScoreQuery.HasAvatarId)
                {
                    Logger.Debug($"GetReport(): playerScoreQuery.AvatarId == {request.PlayerScoreQuery.AvatarId}");
                    scoreData.SetAvatarId(request.PlayerScoreQuery.AvatarId);
                }

                report.SetScoreData(scoreData);
            }

            // TODO: guildScoreQuery - unused?
            if (request.HasGuildScoreQuery)
                Logger.Warn("Unhandled LeaderboardGuildScoreQuery");

            if (request.HasMetaScoreQuery)
            {
                // Tournament: Civil War
                report.SetScoreData(LeaderboardScoreData.CreateBuilder()
                    .SetLeaderboardId((ulong)LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetPlayerId(request.MetaScoreQuery.PlayerId)
                    .SetScore(1000)
                    .SetPercentileBucket((uint)LeaderboardPercentile.Within10Percent));
            }

            if (request.HasDataQuery)
            {
                var metadata = LeaderboardMetadata.CreateBuilder()
                    .SetLeaderboardId((ulong)LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetState(State)
                    .SetActivationTimestampUtc((long)ActivationTimestampUtc.TotalSeconds)
                    .SetExpirationTimestampUtc((long)ActivationTimestampUtc.TotalSeconds)
                    .SetVisible(Visible);

                report.SetTableData(LeaderboardTableData.CreateBuilder()
                    .SetInfo(metadata)
                    .AddRangeEntries(_entryList));
            }

            return report.Build();
        }
    }
}
