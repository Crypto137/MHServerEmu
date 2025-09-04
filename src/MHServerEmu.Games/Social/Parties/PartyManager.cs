using Gazillion;
using MHServerEmu.Core.Logging;
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
            Logger.Debug($"OnClientPartyOperationRequest():\n{request}");

            switch (request.Operation)
            {
                /*
                case GroupingOperationType.eGOP_InvitePlayer:
                    if (player.CanFormParty() == false)
                    {
                        SendOperationResultToClient(player.DatabaseUniqueId, request, GroupingOperationResult.eGOPR_SystemError);
                        return;
                    }

                    SendOperationRequestToPlayerManager(request);
                    break;
                */

                default:
                    Logger.Warn($"OnClientPartyOperationRequest(): Unhandled operation {request.Operation} from player {player}");
                    SendOperationResultToClient(player.DatabaseUniqueId, request, GroupingOperationResult.eGOPR_SystemError);
                    break;
            }
        }

        private bool SendOperationRequestToPlayerManager(PartyOperationPayload request)
        {
            // TODO
            return true;
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
