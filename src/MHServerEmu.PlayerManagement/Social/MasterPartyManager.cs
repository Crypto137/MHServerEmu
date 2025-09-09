using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// The authority on all parties across all game instances on the server.
    /// </summary>
    public class MasterPartyManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, Party> _parties = new();

        private readonly PlayerManagerService _playerManager;

        private ulong _currentPartyId = 0;

        public MasterPartyManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void OnPlayerRemoved(PlayerHandle player)
        {
            player.PendingParty?.CancelInvitation(player);

            if (player.CurrentParty != null)
                RemovePlayerFromParty(player, GroupLeaveReason.GROUP_LEAVE_REASON_DISCONNECTED);
        }

        public GroupingOperationResult DoPartyOperation(ref PartyOperationPayload request, HashSet<PlayerHandle> playersToNotify)
        {
            GroupingOperationResult result = GroupingOperationResult.eGOPR_SystemError;

            // Get requesting player
            ulong requestingPlayerDbId = request.RequestingPlayerDbId;
            PlayerHandle requestingPlayer = _playerManager.ClientManager.GetPlayer(requestingPlayerDbId);
            if (requestingPlayer == null)
                return Logger.WarnReturn(result, $"OnPartyOperationRequest(): Player 0x{requestingPlayerDbId:X} not found");

            playersToNotify.Add(requestingPlayer);

            // Get target player (may not be online, in which case it's going to be null here)
            PlayerHandle targetPlayer;
            if (request.HasTargetPlayerDbId)
            {
                ulong targetPlayerId = request.TargetPlayerDbId;
                targetPlayer = _playerManager.ClientManager.GetPlayer(targetPlayerId);
            }
            else
            {
                targetPlayer = _playerManager.ClientManager.GetPlayer(request.TargetPlayerName);
                if (targetPlayer != null)
                {
                    // Messages in protobuf-csharp-port are immutable, so we have to rebuild the request here to add a target id to it.
                    request = PartyOperationPayload.CreateBuilder()
                        .MergeFrom(request)
                        .SetTargetPlayerDbId(targetPlayer.PlayerDbId)
                        .SetTargetPlayerName(targetPlayer.PlayerName)
                        .Build();
                }
            }

            switch (request.Operation)
            {
                case GroupingOperationType.eGOP_InvitePlayer:
                    result = DoPartyOperationInvitePlayer(requestingPlayer, targetPlayer, playersToNotify);
                    break;

                case GroupingOperationType.eGOP_AcceptInvite:
                    result = DoPartyOperationAcceptInvite(requestingPlayer, playersToNotify);
                    break;

                default:
                    Logger.Warn($"DoPartyOperation(): Unhandled party operation {request.Operation}");
                    break;
            }

            return result;
        }

        #region Operations

        private GroupingOperationResult DoPartyOperationInvitePlayer(PlayerHandle requestingPlayer, PlayerHandle targetPlayer, HashSet<PlayerHandle> playersToNotify)
        {
            if (requestingPlayer == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (targetPlayer == null)
                return GroupingOperationResult.eGOPR_TargetPlayerNotFound;

            if (targetPlayer == requestingPlayer)
                return GroupingOperationResult.eGOPR_TargetedSelf;

            // If this is an available distinct player, include them in the response.
            playersToNotify.Add(targetPlayer);

            if (targetPlayer.CurrentParty != null)
            {
                if (requestingPlayer.CurrentParty != null && requestingPlayer.CurrentParty == targetPlayer.CurrentParty)
                    return GroupingOperationResult.eGOPR_NoChange;

                return GroupingOperationResult.eGOPR_AlreadyInParty;
            }

            if (targetPlayer.PendingParty != null)
                return GroupingOperationResult.eGOPR_AlreadyHasInvite;

            Party party = requestingPlayer.CurrentParty;
            if (party == null)
            {
                // The requesting player will be the leader of the new party by default.
                party = CreateParty(requestingPlayer);
            }
            else
            {
                if (requestingPlayer != party.Leader)
                    return GroupingOperationResult.eGOPR_NotLeader;
            }

            if (party.IsFull())
                return GroupingOperationResult.eGOPR_PartyFull;

            party.AddInvitation(targetPlayer);

            Logger.Info($"DoPartyOperationInvitePlayer(): Success for [{requestingPlayer}] => [{targetPlayer}]");

            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationAcceptInvite(PlayerHandle player, HashSet<PlayerHandle> playersToNotify)
        {
            if (player == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (player.PendingParty == null)
                return GroupingOperationResult.eGOPR_PendingPartyDisbanded;

            if (player.CurrentParty != null)
                return GroupingOperationResult.eGOPR_AlreadyInParty;

            Party party = player.PendingParty;
            if (party.HasInvitation(player) == false)
            {
                player.PendingParty = null;
                return GroupingOperationResult.eGOPR_NoPendingInvite;
            }

            if (party.IsFull())
                return GroupingOperationResult.eGOPR_PartyFull;

            party.AddMember(player);
            party.GetMembers(playersToNotify);  // notify all members
            
            return GroupingOperationResult.eGOPR_Success;
        }

        #endregion

        #region Party Management

        private Party CreateParty(PlayerHandle player)
        {
            if (player == null) return Logger.WarnReturn<Party>(null, "CreateParty(): player == null");
            if (player.CurrentParty != null) return Logger.WarnReturn<Party>(null, "CreateParty(): player.CurrentParty != null");

            Party party = new(++_currentPartyId, player);
            _parties.Add(party.Id, party);

            Logger.Info($"CreateParty(): Created party [{party}]");

            return party;
        }

        private void RemovePlayerFromParty(PlayerHandle player, GroupLeaveReason reason)
        {
            Party party = player.CurrentParty;
            if (party == null)
                return;

            party.RemoveMember(player, reason);
        }

        #endregion
    }
}
