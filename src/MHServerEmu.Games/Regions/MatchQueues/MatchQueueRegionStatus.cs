using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    /// <summary>
    /// Represents a group of players queued for a specific region and difficulty tier in <see cref="MatchQueueStatus"/>.
    /// </summary>
    public class MatchQueueRegionStatus
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public PrototypeId RegionRef { get; }
        public PrototypeId DifficultyTierRef { get; }
        public ulong GroupId { get; set; }

        public Dictionary<ulong, MatchQueuePlayerInfoEntry> PlayerInfos { get; } = new();

        public int PlayersInQueue { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="MatchQueueRegionStatus"/> for the specified region / difficulty tier combination.
        /// </summary>
        public MatchQueueRegionStatus(PrototypeId regionRef, PrototypeId difficultyTierRef, ulong groupId)
        {
            RegionRef = regionRef;
            DifficultyTierRef = difficultyTierRef;
            GroupId = groupId;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(GroupId)}: {GroupId}");

            foreach (var kvp in PlayerInfos)
                sb.AppendLine($"[{kvp.Key}] {kvp.Value}");

            return sb.ToString();
        }

        public void UpdateQueue(int playersInQueue)
        {
            if (playersInQueue < 0)
            {
                Logger.Warn("UpdateQueue(): playersInQueue < 0");
                return;
            }

            PlayersInQueue = playersInQueue;
        }

        /// <summary>
        /// Updates the <see cref="RegionRequestQueueUpdateVar"/> value of the specified player.
        /// </summary>
        public bool UpdatePlayer(ulong playerGuid, RegionRequestQueueUpdateVar status, string playerName)
        {
            // Some statuses cause the player to be removed.
            if (MatchQueueStatus.IsRemovePlayerStatus(status))
                return PlayerInfos.Remove(playerGuid);

            if (PlayerInfos.TryGetValue(playerGuid, out var entry) == false)
            {
                entry = new(playerName, status);
                PlayerInfos.Add(playerGuid, entry);
                return true;
            }

            return entry.Update(status);
        }
    }
}
