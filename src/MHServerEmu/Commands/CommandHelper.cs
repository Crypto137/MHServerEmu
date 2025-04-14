using MHServerEmu.Core.Network;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;
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

            playerConnection = game.NetworkManager.GetNetClient(client);
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

        /// <summary>
        /// Searches for a <see cref="PrototypeId"/> that matches the specified <see cref="BlueprintId"/> and <see cref="string"/> pattern.
        /// Returns <see cref="PrototypeId.Invalid"/> if no or more than one match is found.
        /// </summary>
        public static PrototypeId FindPrototype(BlueprintId blueprintRef, string pattern, FrontendClient client)
        {
            const int MaxMatches = 10;

            IEnumerable<PrototypeId> matches = GameDatabase.SearchPrototypes(pattern,
                DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprintRef);

            // Not enough
            if (matches.Any() == false)
            {
                ChatHelper.SendMetagameMessage(client, $"Failed to find any {((PrototypeId)blueprintRef).GetNameFormatted()} prototypes containing {pattern}.");
                return PrototypeId.Invalid;
            }

            // Too many
            int numMatches = matches.Count();
            if (numMatches > 1)
            {
                var matchNames = matches.Select(match => GameDatabase.GetPrototypeName(match));

                if (numMatches <= MaxMatches)
                {
                    ChatHelper.SendMetagameMessage(client, $"Found multiple matches for {pattern}:");
                    ChatHelper.SendMetagameMessages(client, matchNames, false);
                }
                else
                {
                    ChatHelper.SendMetagameMessage(client, $"Found over {MaxMatches} matches for {pattern}, here are the first {MaxMatches}:");
                    ChatHelper.SendMetagameMessages(client, matchNames.Take(MaxMatches), false);
                }

                return PrototypeId.Invalid;
            }

            return matches.First();
        }
    }
}
