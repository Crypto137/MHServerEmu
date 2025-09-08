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
            if (_playerManager.ClientManager.TryGetPlayerHandle(requestingPlayerDbId, out PlayerHandle requestingPlayer) == false)
                return Logger.WarnReturn(result, $"OnPartyOperationRequest(): Player 0x{requestingPlayerDbId:X} not found");

            playersToNotify.Add(requestingPlayer);

            // Get target player
            ulong targetPlayerId = 0;
            if (request.HasTargetPlayerDbId)
            {
                targetPlayerId = request.TargetPlayerDbId;
            }
            else
            {
                // TODO: Check online players only instead of using the database cache
                if (PlayerNameCache.Instance.TryGetPlayerDbId(request.TargetPlayerName, out targetPlayerId, out string resultPlayerName))
                {
                    // Messages in protobuf-csharp-port are immutable, so we have to rebuild the request here to add a target id to it.
                    request = PartyOperationPayload.CreateBuilder()
                        .MergeFrom(request)
                        .SetTargetPlayerDbId(targetPlayerId)
                        .SetTargetPlayerName(resultPlayerName)
                        .Build();
                }
            }

            // The target player may not be online, in which case it's going to be null here.
            _playerManager.ClientManager.TryGetPlayerHandle(targetPlayerId, out PlayerHandle targetPlayer);

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
