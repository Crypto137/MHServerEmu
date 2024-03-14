using System.Text;
using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    /// <summary>
    /// Represents a group of players queued for a specific region and difficulty tier in <see cref="MatchQueueStatus"/>.
    /// </summary>
    public class MatchQueueRegionStatus
    {
        public PrototypeId RegionRef { get; }
        public PrototypeId DifficultyTierRef { get; }
        public ulong RegionRequestGroupId { get; set; }

        public Dictionary<ulong, MatchQueuePlayerInfoEntry> PlayerInfoDict { get; } = new();

        /// <summary>
        /// Constructs a new <see cref="MatchQueueRegionStatus"/> for the specified region / difficulty tier combination.
        /// </summary>
        public MatchQueueRegionStatus(PrototypeId regionRef, PrototypeId difficultyTierRef, ulong regionRequestGroupId)
        {
            RegionRef = regionRef;
            DifficultyTierRef = difficultyTierRef;
            RegionRequestGroupId = regionRequestGroupId;
        }

        /// <summary>
        /// Updates the <see cref="RegionRequestQueueUpdateVar"/> value of the specified player.
        /// </summary>
        public bool UpdatePlayer(ulong playerGuid, RegionRequestQueueUpdateVar status, string playerName)
        {
            // Remove player if needed
            if (MatchQueueStatus.RemovePlayerOnStatus(status))
                return PlayerInfoDict.Remove(playerGuid);

            // Create a new entry with the specified status if there is no existing one
            if (PlayerInfoDict.TryGetValue(playerGuid, out var entry) == false)
            {
                entry = new(playerName, status);
                PlayerInfoDict.Add(playerGuid, entry);
            }

            // Update the existing entry
            return entry.Update(status);
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(RegionRequestGroupId)}: {RegionRequestGroupId}");

            foreach (var kvp in PlayerInfoDict)
                sb.AppendLine($"[{kvp.Key}] {kvp.Value}");

            return sb.ToString();
        }
    }
}
