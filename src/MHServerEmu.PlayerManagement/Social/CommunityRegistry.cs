using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// Holds community data for all players. This is like the mega-community of all communities on the server.
    /// </summary>
    public class CommunityRegistry
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PlayerManagerService _playerManager;

        public CommunityRegistry(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            // TODO: Send broadcasts
        }

        public bool ReceiveMemberBroadcast(CommunityMemberBroadcast broadcast)
        {
            ulong playerDbId = broadcast.MemberPlayerDbId;

            // We should be receiving broadcasts only from online players
            if (_playerManager.ClientManager.TryGetPlayerHandle(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"ReceiveMemberBroadcast(): No player found for dbid 0x{playerDbId:X}");

            Logger.Debug($"ReceiveMemberBroadcast(): {broadcast}");

            return true;
        }

        public void UpdateSubscription(CommunitySubscriptionOpType operation, ulong subscriberPlayerDbId, ulong targetPlayerDbId)
        {
            // TODO
            Logger.Debug($"UpdateSubscription(): operation={operation}, subscriber=0x{subscriberPlayerDbId:X}, target=0x{targetPlayerDbId:X}");
        }

        public void RequestMemberBroadcast(ulong gameId, ulong playerDbId, List<ulong> members)
        {
            // TODO
            Logger.Debug($"RequestStatus(): gameId=0x{gameId:X}, playerDbId=0x{playerDbId:X}, members={members.Count}");
        }
    }
}
