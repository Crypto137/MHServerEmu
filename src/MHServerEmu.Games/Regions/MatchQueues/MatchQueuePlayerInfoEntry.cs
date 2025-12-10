using Gazillion;

namespace MHServerEmu.Games.Regions.MatchQueues
{
    /// <summary>
    /// Represents a player in a group contained in <see cref="MatchQueueRegionStatus"/>.
    /// </summary>
    public class MatchQueuePlayerInfoEntry
    {
        public string PlayerName { get; }
        public RegionRequestQueueUpdateVar Status { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="MatchQueuePlayerInfoEntry"/> instance.
        /// </summary>
        public MatchQueuePlayerInfoEntry(string playerName, RegionRequestQueueUpdateVar status)
        {
            PlayerName = playerName;
            Status = status;
        }

        public override string ToString()
        {
            return $"{PlayerName}: {Status}";
        }

        /// <summary>
        /// Updates the <see cref="RegionRequestQueueUpdateVar"/> value of this <see cref="MatchQueuePlayerInfoEntry"/>. Returns <see langword="true"/> if status changed.
        /// </summary>
        public bool Update(RegionRequestQueueUpdateVar status)
        {
            if (Status == status)
                return false;

            Status = status;
            return true;
        }
    }
}
