using MHServerEmu.Core.Network;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Provides common helper functions for commands.
    /// </summary>
    public static class CommandHelper
    {
        /// <summary>
        /// Retrieves the current <see cref="Game"/> instance for the provided <see cref="FrontendClient."/>
        /// Return <see langword="true"/> if successful.
        /// </summary>
        public static bool TryGetGame(FrontendClient client, out Game game)
        {
            game = null;

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null) return false;

            game = playerManager.GetGameByPlayer(client);
            return game != null;
        }

        /// <summary>
        /// Retrieves the current <see cref="PlayerConnection"/> and <see cref="Game"/> instances for the provided <see cref="FrontendClient."/>
        /// Return <see langword="true"/> if successful.
        /// </summary>
        public static bool TryGetPlayerConnection(FrontendClient client, out PlayerConnection playerConnection, out Game game)
        {
            playerConnection = null;

            if (TryGetGame(client, out game) == false)
                return false;

            playerConnection = game.NetworkManager.GetPlayerConnection(client);
            return playerConnection != null;
        }

        /// <summary>
        /// Retrieves the current <see cref="PlayerConnection"/> instance for the provided <see cref="FrontendClient."/>
        /// Return <see langword="true"/> if successful.
        /// </summary>
        public static bool TryGetPlayerConnection(FrontendClient client, out PlayerConnection playerConnection)
        {
            return TryGetPlayerConnection(client, out playerConnection, out _);
        }
    }
}
