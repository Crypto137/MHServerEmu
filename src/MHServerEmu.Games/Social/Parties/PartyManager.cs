using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social.Parties
{
    public class PartyManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public PartyManager(Game game)
        {
            Game = game;
        }

        public void OnClientPartyOperationRequest(Player player, PartyOperationPayload request)
        {
            switch (request.Operation)
            {
                case GroupingOperationType.eGOP_InvitePlayer:
                    if (player.CanFormParty() == false)
                    {
                        SendOperationResultToClient(player.DatabaseUniqueId, request, GroupingOperationResult.eGOPR_SystemError);
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
                        SendOperationResultToClient(player.DatabaseUniqueId, request, GroupingOperationResult.eGOPR_NotLeader);
                        return;
                    }

                    SendOperationRequestToPlayerManager(request);
                    break;

                //case GroupingOperationType.eGOP_ConvertToRaidAccept:
                //case GroupingOperationType.eGOP_ConvertToRaidDecline:
                    // TODO

                default:
                    Logger.Warn($"OnClientPartyOperationRequest(): Unhandled operation {request.Operation} from player {player}");
                    SendOperationResultToClient(player.DatabaseUniqueId, request, GroupingOperationResult.eGOPR_SystemError);
                    break;
            }
        }

        public void OnPartyOperationRequestServerResult(ulong playerDbId, PartyOperationPayload request, GroupingOperationResult result)
        {
            Logger.Debug($"OnPartyOperationRequestServerResult(): {request.Operation} {request.RequestingPlayerName} => {request.TargetPlayerName}: {result}");
            SendOperationResultToClient(playerDbId, request, result);
        }

        public void OnPartyInfoServerUpdate(ulong playerDbId, ulong groupId, PartyInfo partyInfo)
        {
            Logger.Debug($"OnPartyInfoServerUpdate(): {partyInfo}");
        }

        public void OnPartyMemberInfoServerUpdate(ulong playerDbId, ulong groupId, ulong memberDbId, PartyMemberEvent memberEvent, Gazillion.PartyMemberInfo partyMemberInfo)
        {
            Logger.Debug($"OnPartyMemberInfoServerUpdate(): {partyMemberInfo}");
        }

        private void SendOperationRequestToPlayerManager(PartyOperationPayload request)
        {
            ServiceMessage.PartyOperationRequest message = new(request);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, message);
        }

        private bool SendOperationResultToClient(ulong playerDbId, PartyOperationPayload request, GroupingOperationResult result)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
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
