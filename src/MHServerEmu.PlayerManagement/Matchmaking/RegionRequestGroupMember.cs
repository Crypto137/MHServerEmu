using Gazillion;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class RegionRequestGroupMember
    {
        public RegionRequestGroup Group { get; }
        public PlayerHandle Player { get; }
        public RegionRequestQueueUpdateVar Status { get; set; }

        public RegionRequestGroupMember(RegionRequestGroup group, PlayerHandle player)
        {
            Group = group;
            Player = player;
            Status = RegionRequestQueueUpdateVar.eRRQ_Invalid;
        }
    }
}
