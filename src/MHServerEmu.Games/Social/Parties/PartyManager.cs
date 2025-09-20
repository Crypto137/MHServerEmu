using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social.Parties
{
    public class PartyManager
    {
        // NOTE: It seems client-side party management functions are merged with ClientGame.

        private static readonly Logger Logger = LogManager.CreateLogger();

        // This is a local cache of the authoritative data from the PlayerManager. May be out of date.
        private readonly Dictionary<ulong, Party> _localParties = new();

        public Game Game { get; }

        public PartyManager(Game game)
        {
            Game = game;
        }

        public Party GetParty(ulong partyId)
        {
            if (_localParties.TryGetValue(partyId, out Party party) == false)
                return null;

            return party;
        }

        public void OnClientPartyOperationRequest(Player player, PartyOperationPayload request)
        {
            Party party = player.GetParty();

            switch (request.Operation)
            {
                case GroupingOperationType.eGOP_InvitePlayer:
                    if (player.CanFormParty() == false)
                    {
                        SendOperationResultToClient(player, request, GroupingOperationResult.eGOPR_SystemError);
                        return;
                    }

                    SendOperationRequestToPlayerManager(request);
                    break;

                case GroupingOperationType.eGOP_AcceptInvite:
                case GroupingOperationType.eGOP_DeclineInvite:
                case GroupingOperationType.eGOP_LeaveParty:
                    // No extra validation required here.
                    SendOperationRequestToPlayerManager(request);
                    break;

                case GroupingOperationType.eGOP_DisbandParty:
                case GroupingOperationType.eGOP_KickPlayer:
                case GroupingOperationType.eGOP_ChangeLeader:
                case GroupingOperationType.eGOP_ConvertToRaid:
                case GroupingOperationType.eGOP_ConvertToParty:
                    // Need to be a party leader for these.
                    if (player.IsPartyLeader() == false)
                    {
                        SendOperationResultToClient(player, request, GroupingOperationResult.eGOPR_NotLeader);
                        return;
                    }

                    SendOperationRequestToPlayerManager(request);
                    break;

                case GroupingOperationType.eGOP_ConvertToRaidAccept:
                    if (party != null)
                        player.BeginTeleportToPartyMember(party.LeaderId);
                    break;

                case GroupingOperationType.eGOP_ConvertToRaidDecline:
                    // Boot this ungrateful player from the party for refusing to comply with the benevolent leader's demands.
                    PartyOperationPayload leaveRequest = PartyOperationPayload.CreateBuilder()
                        .MergeFrom(request)
                        .SetOperation(GroupingOperationType.eGOP_LeaveParty)
                        .Build();
                    SendOperationRequestToPlayerManager(leaveRequest);
                    break;

                default:
                    Logger.Warn($"OnClientPartyOperationRequest(): Unhandled operation {request.Operation} from player {player}");
                    SendOperationResultToClient(player, request, GroupingOperationResult.eGOPR_SystemError);
                    break;
            }
        }

        public bool OnPartyOperationRequestServerResult(ulong playerDbId, PartyOperationPayload request, GroupingOperationResult result)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnPartyOperationRequestServerResult(): player == null");

            switch (request.Operation)
            {
                case GroupingOperationType.eGOP_InvitePlayer:
                    // Automatically decline invites from ignored players.
                    CommunityMember member = player.Community.GetMember(request.RequestingPlayerDbId);
                    if (member != null && member.IsIgnored())
                    {
                        PartyOperationPayload declineRequest = PartyOperationPayload.CreateBuilder()
                            .SetRequestingPlayerDbId(playerDbId)
                            .SetRequestingPlayerName(player.GetName())
                            .SetOperation(GroupingOperationType.eGOP_DeclineInvite)
                            .SetDifficultyTierProtoId(request.DifficultyTierProtoId)
                            .Build();

                        SendOperationRequestToPlayerManager(declineRequest);
                        return true;
                    }
                    break;

                case GroupingOperationType.eGOP_DeclineInvite:
                    // The client interprets decline requests as if they are coming from the target player,
                    // so don't forward this back to the declining player.
                    if (request.RequestingPlayerDbId == player.DatabaseUniqueId)
                        return true;
                    break;
            }

            SendOperationResultToClient(player, request, result);
            return true;
        }

        public void OnPartyInfoServerUpdate(ulong playerDbId, ulong groupId, PartyInfo partyInfo)
        {
            if (partyInfo != null)
                CreateOrUpdateParty(partyInfo);

            // Relay update to the player
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player != null)
            {
                var clientUpdate = PartyInfoClientUpdate.CreateBuilder()
                    .SetGroupId(groupId);

                if (partyInfo != null)
                    clientUpdate.SetPartyInfo(partyInfo);

                player.SendMessage(clientUpdate.Build());
            }
        }

        public void OnPartyMemberInfoServerUpdate(ulong playerDbId, ulong groupId, ulong memberDbId, PartyMemberEvent memberEvent, Gazillion.PartyMemberInfo memberInfo)
        {
            // Update local party
            Party party = GetParty(groupId);
            if (party != null)
            {
                switch (memberEvent)
                {
                    case PartyMemberEvent.ePME_Add:
                        party.AddMember(memberInfo);
                        break;

                    case PartyMemberEvent.ePME_Remove:
                        party.RemoveMember(memberDbId, GroupLeaveReason.GROUP_LEAVE_REASON_LEFT);
                        break;

                    case PartyMemberEvent.ePME_Update:
                        party.UpdateMember(memberInfo);
                        break;
                }

                TryCleanUpParty(groupId);
            }

            // Relay update to the player
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player != null)
            {
                var clientUpdate = PartyMemberInfoClientUpdate.CreateBuilder()
                    .SetGroupId(groupId)
                    .SetMemberDbGuid(memberDbId)
                    .SetMemberEvent(memberEvent);

                if (memberInfo != null)
                    clientUpdate.SetMemberInfo(memberInfo);

                player.SendMessage(clientUpdate.Build());
            }
        }

        public void OnPlayerEnteredRegion(Player player)
        {
            foreach (Party party in _localParties.Values)
            {
                if (party.IsMember(player))
                {
                    player.RefreshPartyOnRegionEnter(party);
                    return;
                }
            }
        }

        public void OnPlayerDestroyed(Player player)
        {
            ulong partyId = player.PartyId;
            if (partyId != 0)
                TryCleanUpParty(partyId);
        }

        private Party CreateOrUpdateParty(PartyInfo partyInfo)
        {
            ulong partyId = partyInfo.GroupId;

            if (_localParties.TryGetValue(partyId, out Party localParty) == false)
            {
                localParty = new(partyId);
                _localParties.Add(partyId, localParty);
                Logger.Info($"Added party 0x{partyId:X} to game 0x{Game.Id:X}");
            }

            localParty.SetFromMessage(partyInfo);

            return localParty;
        }

        private void TryCleanUpParty(ulong partyId)
        {
            if (_localParties.TryGetValue(partyId, out Party party) == false)
                return;

            // Do not remove if any of the players is still in this game instance.
            EntityManager entityManager = Game.EntityManager;
            foreach (var kvp in party)
            {
                ulong playerDbId = kvp.Value.PlayerDbId;
                Player player = entityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player != null)
                    return;
            }

            _localParties.Remove(partyId);

            Logger.Info($"Removed party 0x{partyId:X} from game 0x{Game.Id:X}");
        }

        private static void SendOperationRequestToPlayerManager(PartyOperationPayload request)
        {
            ServiceMessage.PartyOperationRequest message = new(request);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        private static bool SendOperationResultToClient(Player player, PartyOperationPayload request, GroupingOperationResult result)
        {
            if (player == null) return Logger.WarnReturn(false, "SendOperationResultToClient(): player == null");

            PartyOperationRequestClientResult message = PartyOperationRequestClientResult.CreateBuilder()
                .SetRequest(request)
                .SetResult(result)
                .Build();

            player.SendMessage(message);
            return true;
        }
    }
}
