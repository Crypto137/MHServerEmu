using MHServerEmu.Core.System.Time;
using MHServerEmu.PlayerManagement.Matchmaking;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement
{
    public class PlayerManagerEventScheduler
    {
        // TODO: Add an event for WorldView grace period expiration after leaving a party when we implement that.

        public ServiceEventScheduler<ulong, RegionRequestGroupState> MatchmakingGroupStateChange { get; } = new();
        public ServiceEventScheduler<ulong, bool> MatchmakingGroupStateUpdate { get; } = new();
        public ServiceEventScheduler<ulong, PlayerHandle> MatchmakingGroupInviteExpired { get; } = new();
        public ServiceEventScheduler<ulong, PlayerHandle> MatchmakingMatchInviteExpired { get; } = new();
        public ServiceEventScheduler<ulong, PlayerHandle> MatchmakingRemovedGracePeriodExpired { get; } = new();

        public PlayerManagerEventScheduler()
        {
        }

        public void TriggerEvents()
        {
            MatchmakingGroupStateChange.TriggerEvents();
            MatchmakingGroupStateUpdate.TriggerEvents();
            MatchmakingGroupInviteExpired.TriggerEvents();
            MatchmakingMatchInviteExpired.TriggerEvents();
            MatchmakingRemovedGracePeriodExpired.TriggerEvents();
        }
    }
}
