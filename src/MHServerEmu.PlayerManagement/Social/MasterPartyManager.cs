using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// The authority on all parties across all game instances on the server.
    /// </summary>
    public class MasterPartyManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, MasterParty> _parties = new();

        private readonly PlayerManagerService _playerManager;

        private ulong _currentPartyId = 0;

        public MasterPartyManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void OnPlayerRegionTransferFinished(PlayerHandle player)
        {
            // The client "loses" pending party invites on region change, so just cancel it here as well.
            // This won't do anything if there isn't an actual pending invite.
            CancelPartyInvite(player);

            // Sync party info
            if (player.CurrentParty != null)
            {
                player.CurrentParty.SyncPartyInfo(player);
            }
            else
            {
                // No party (we are assuming CurrentGame is not null because this is a callback for a transfer confirmation)
                ServiceMessage.PartyInfoServerUpdate message = new(player.CurrentGame.Id, player.PlayerDbId, 0, null);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        public void OnPlayerRemoved(PlayerHandle player)
        {
            CancelPartyInvite(player);
            RemoveMemberFromParty(player, GroupLeaveReason.GROUP_LEAVE_REASON_DISCONNECTED);
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

            PrototypeId difficultyTierProtoRef = request.HasDifficultyTierProtoId ? (PrototypeId)request.DifficultyTierProtoId : 0;

            switch (request.Operation)
            {
                case GroupingOperationType.eGOP_InvitePlayer:
                    result = DoPartyOperationInvitePlayer(requestingPlayer, targetPlayer);
                    if (result == GroupingOperationResult.eGOPR_Success)
                        playersToNotify.Add(targetPlayer);
                    break;

                case GroupingOperationType.eGOP_AcceptInvite:
                    requestingPlayer.PendingParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationAcceptInvite(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_DeclineInvite:
                    requestingPlayer.PendingParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationDeclineInvite(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_LeaveParty:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationLeaveParty(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_DisbandParty:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationDisbandParty(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_KickPlayer:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationKickPlayer(requestingPlayer, targetPlayer);
                    break;

                case GroupingOperationType.eGOP_ChangeLeader:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationChangeLeader(requestingPlayer, targetPlayer);
                    break;

                case GroupingOperationType.eGOP_ConvertToRaid:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationConvertToRaid(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_ConvertToParty:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationConvertToParty(requestingPlayer);
                    break;

                case GroupingOperationType.eGOP_ChangeDifficulty:
                    requestingPlayer.CurrentParty?.GetMembers(playersToNotify);
                    result = DoPartyOperationChangeDifficulty(requestingPlayer, difficultyTierProtoRef);
                    break;

                default:
                    Logger.Warn($"DoPartyOperation(): Unhandled party operation {request.Operation}");
                    break;
            }

            return result;
        }

        #region Operations

        private GroupingOperationResult DoPartyOperationInvitePlayer(PlayerHandle requestingPlayer, PlayerHandle targetPlayer)
        {
            if (requestingPlayer == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (targetPlayer == null)
                return GroupingOperationResult.eGOPR_TargetPlayerNotFound;

            if (targetPlayer == requestingPlayer)
                return GroupingOperationResult.eGOPR_TargetedSelf;

            if (targetPlayer.HasVisitedTown == false)
                return GroupingOperationResult.eGOPR_HasNoCheckpoint;

            if (targetPlayer.CurrentParty != null)
            {
                if (requestingPlayer.CurrentParty != null && requestingPlayer.CurrentParty == targetPlayer.CurrentParty)
                    return GroupingOperationResult.eGOPR_NoChange;

                return GroupingOperationResult.eGOPR_AlreadyInParty;
            }

            if (targetPlayer.PendingParty != null)
                return GroupingOperationResult.eGOPR_AlreadyHasInvite;

            MasterParty party = requestingPlayer.CurrentParty;
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

            party.AddInvite(targetPlayer);

            Logger.Info($"DoPartyOperationInvitePlayer(): Success for [{requestingPlayer}] => [{targetPlayer}]");

            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationAcceptInvite(PlayerHandle player)
        {
            if (player == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (player.PendingParty == null)
                return GroupingOperationResult.eGOPR_PendingPartyDisbanded;

            if (player.CurrentParty != null)
                return GroupingOperationResult.eGOPR_AlreadyInParty;

            MasterParty party = player.PendingParty;
            if (party.HasInvite(player) == false)
            {
                player.PendingParty = null;
                return GroupingOperationResult.eGOPR_NoPendingInvite;
            }

            if (party.IsFull())
                return GroupingOperationResult.eGOPR_PartyFull;

            party.AddMember(player);
            
            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationDeclineInvite(PlayerHandle player)
        {
            if (player == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (player.PendingParty == null)
                return GroupingOperationResult.eGOPR_PendingPartyDisbanded;

            MasterParty party = player.PendingParty;
            if (party.HasInvite(player) == false)
                return GroupingOperationResult.eGOPR_NoPendingInvite;

            CancelPartyInvite(player);

            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationLeaveParty(PlayerHandle player)
        {
            if (player == null)
                return GroupingOperationResult.eGOPR_SystemError;

            MasterParty party = player.CurrentParty;
            if (party == null)
                return GroupingOperationResult.eGOPR_NotInParty;

            RemoveMemberFromParty(player, GroupLeaveReason.GROUP_LEAVE_REASON_LEFT);
            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationDisbandParty(PlayerHandle player)
        {
            GroupingOperationResult result = ValidatePartyLeader(player);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            DisbandParty(player.CurrentParty);
            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationKickPlayer(PlayerHandle requestingPlayer, PlayerHandle targetPlayer)
        {
            if (targetPlayer == null)
                return GroupingOperationResult.eGOPR_TargetPlayerNotFound;

            GroupingOperationResult result = ValidatePartyLeader(requestingPlayer);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            RemoveMemberFromParty(targetPlayer, GroupLeaveReason.GROUP_LEAVE_REASON_BOOTED);
            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationChangeLeader(PlayerHandle requestingPlayer, PlayerHandle targetPlayer)
        {
            if (targetPlayer == null)
                return GroupingOperationResult.eGOPR_TargetPlayerNotFound;

            GroupingOperationResult result = ValidatePartyLeader(requestingPlayer);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            requestingPlayer.CurrentParty.SetLeader(targetPlayer);
            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationConvertToRaid(PlayerHandle player)
        {
            GroupingOperationResult result = ValidatePartyLeader(player);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            MasterParty party = player.CurrentParty;

            if (party.SetType(GroupType.GroupType_Raid) == false)
                return GroupingOperationResult.eGOPR_NoChange;

            foreach (PlayerHandle member in party)
                member.CheckWorldViewRegionAvailability();

            return GroupingOperationResult.eGOPR_Success;           
        }

        private GroupingOperationResult DoPartyOperationConvertToParty(PlayerHandle player)
        {
            GroupingOperationResult result = ValidatePartyLeader(player);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            MasterParty party = player.CurrentParty;

            if (party.MemberCount > GameDatabase.GlobalsPrototype.PlayerPartyMaxSize)
                return GroupingOperationResult.eGOPR_PartyFull;

            if (party.SetType(GroupType.GroupType_Party) == false)
                return GroupingOperationResult.eGOPR_NoChange;

            foreach (PlayerHandle member in party)
                member.CheckWorldViewRegionAvailability();

            return GroupingOperationResult.eGOPR_Success;
        }

        private GroupingOperationResult DoPartyOperationChangeDifficulty(PlayerHandle player, PrototypeId difficultyTierProtoRef)
        {
            GroupingOperationResult result = ValidatePartyLeader(player);
            if (result != GroupingOperationResult.eGOPR_Success)
                return result;

            if (difficultyTierProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(GroupingOperationResult.eGOPR_SystemError, "DoPartyOperationChangeDifficulty(): difficultyTierProtoRef == PrototypeId.Invalid");

            // CurrentParty is null checked in ValidatePartyLeader()
            if (player.CurrentParty.SetDifficultyTier(difficultyTierProtoRef) == false)
                return GroupingOperationResult.eGOPR_NoChange;

            return GroupingOperationResult.eGOPR_Success;
        }

        #endregion

        #region Party Management

        private MasterParty CreateParty(PlayerHandle player)
        {
            if (player == null) return Logger.WarnReturn<MasterParty>(null, "CreateParty(): player == null");
            if (player.CurrentParty != null) return Logger.WarnReturn<MasterParty>(null, "CreateParty(): player.CurrentParty != null");

            MasterParty party = new(++_currentPartyId, player);
            _parties.Add(party.Id, party);

            Logger.Info($"CreateParty(): party=[{party}]");

            return party;
        }

        private bool DisbandParty(MasterParty party)
        {
            if (party == null) return Logger.WarnReturn(false, "DisbandParty(): party == null");

            // Clean up remaining members
            HashSet<PlayerHandle> members = HashSetPool<PlayerHandle>.Instance.Get();
            party.GetMembers(members);

            foreach (PlayerHandle member in members)
                party.RemoveMember(member, GroupLeaveReason.GROUP_LEAVE_REASON_DISBANDED);

            if (party.MemberCount == 0)
            {
                // Clear world view and cancel reservations
                party.WorldView.Clear();

                // Cancel invitations
                party.CancelAllInvites();

                // Remove from the manager
                _parties.Remove(party.Id);

                Logger.Info($"DisbandParty(): party=[{party}]");
            }
            else
            {
                Logger.Warn($"DisbandParty(): Failed to remove all players from party {party}");
            }

            foreach (PlayerHandle member in members)
                member.CheckWorldViewRegionAvailability();

            HashSetPool<PlayerHandle>.Instance.Return(members);
            return true;
        }

        private void CancelPartyInvite(PlayerHandle player)
        {
            MasterParty pendingParty = player.PendingParty;
            if (pendingParty == null)
                return;

            pendingParty.RemoveInvite(player);

            if (pendingParty.HasEnoughMembersOrInvitations == false)
                DisbandParty(pendingParty);
        }

        private void RemoveMemberFromParty(PlayerHandle player, GroupLeaveReason reason)
        {
            MasterParty party = player.CurrentParty;
            if (party == null)
                return;

            party.RemoveMember(player, reason);
            player.CheckWorldViewRegionAvailability();

            // If the leader is the only one remaining in the party and there are no pending invites, it's time to disband.
            if (party.HasEnoughMembersOrInvitations == false)
            {
                DisbandParty(party);
                return;
            }

            // Monarchy time: pass leadership to the next in line.
            if (player == party.Leader)
            {
                PlayerHandle nextLeader = party.GetNextLeader();
                party.SetLeader(nextLeader);
            }

            foreach (PlayerHandle member in party)
                member.CheckWorldViewRegionAvailability();
        }

        #endregion

        private static GroupingOperationResult ValidatePartyLeader(PlayerHandle player)
        {
            if (player == null)
                return GroupingOperationResult.eGOPR_SystemError;

            if (player.CurrentParty == null)
                return GroupingOperationResult.eGOPR_NotInParty;

            if (player.CurrentParty.Leader != player)
                return GroupingOperationResult.eGOPR_NotLeader;

            return GroupingOperationResult.eGOPR_Success;
        }
    }
}
