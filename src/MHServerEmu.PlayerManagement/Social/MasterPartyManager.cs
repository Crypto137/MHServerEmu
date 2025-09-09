using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
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
                    if (targetPlayer != null)
                    {
                        playersToNotify.Add(targetPlayer);
                        result = GroupingOperationResult.eGOPR_Success;
                    }
                    break;

                default:
                    Logger.Warn($"DoPartyOperation(): Unhandled party operation {request.Operation}");
                    break;
            }

            return result;
        }
    }
}
