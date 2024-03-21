using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Manages <see cref="Leaderboard"/> instances.
    /// </summary>
    public class LeaderboardManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<(PrototypeGuid, ulong), Leaderboard> _leaderboardDict = new();

        public int LeaderboardCount { get => _leaderboardDict.Count; }

        /// <summary>
        /// Returns the specified <see cref="Leaderboard"/> instance.
        /// </summary>
        public Leaderboard GetLeaderboard(PrototypeGuid guid, ulong instanceId)
        {
            // Multiple instances of the same leaderboard are currently not supported.
            if (instanceId != 0)
                return Logger.WarnReturn<Leaderboard>(null, $"GetLeaderboard(): Unexpected leaderboard instanceId {instanceId}");

            // Create a new leaderboard if not found
            if (_leaderboardDict.TryGetValue((guid, instanceId), out var leaderboard) == false)
            {
                leaderboard = new(guid,
                    instanceId,
                    LeaderboardState.eLBS_Active,
                    Clock.UnixTime - TimeSpan.FromDays(1),
                    Clock.UnixTime + TimeSpan.FromDays(1),
                    true);

                _leaderboardDict.Add((guid, instanceId), leaderboard);
            }

            return leaderboard;
        }

        /// <summary>
        /// Returns <see cref="LeaderboardInitData"/> for all instances of the specified leaderboard.
        /// </summary>
        public LeaderboardInitData GetLeaderboardInitData(PrototypeGuid guid)
        {
            // TODO: archived instance data
            Leaderboard leaderboard = GetLeaderboard(guid, 0);

            return LeaderboardInitData.CreateBuilder()
                .SetLeaderboardId((ulong)guid)
                .SetCurrentInstanceData(leaderboard.GetInstanceData())
                .Build();
        }
    }
}
