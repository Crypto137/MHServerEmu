using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
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

        public MasterPartyManager(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public bool ReceivePartyOperationRequest(PartyOperationPayload request)
        {
            if (_playerManager.ClientManager.TryGetPlayerHandle(request.RequestingPlayerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnPartyOperationRequest(): Player 0x{request.RequestingPlayerDbId:X} not found");

            ServiceMessage.PartyOperationRequestServerResult result = new(
                player.CurrentGame.Id, player.PlayerDbId, request, GroupingOperationResult.eGOPR_SystemError);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, result);

            Logger.Debug(request.ToString());

            return true;
        }
    }
}
