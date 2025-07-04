using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement.Handles
{
    public class PlayerHandleManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PlayerHandle> _playerDict = new();

        public PlayerHandleManager() { }

        public bool TryGetPlayer(IFrontendClient client, out PlayerHandle player)
        {
            return _playerDict.TryGetValue(client.DbId, out player);
        }

        public bool AddPlayer(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_playerDict.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _playerDict.Add(playerDbId, player);
            }
            else
            {
                // TODO: Transfer handle between clients
                client.Disconnect();
                player = null;
                return Logger.ErrorReturn(false, $"AddPlayer(): PlayerDbId 0x{playerDbId:X} is already in use by another client");
            }

            return true;
        }

        public bool RemovePlayer(IFrontendClient client)
        {
            ulong playerDbId = client.DbId;

            if (_playerDict.Remove(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): Client [{client}] is not bound to a player");

            return true;
        }
    }
}
