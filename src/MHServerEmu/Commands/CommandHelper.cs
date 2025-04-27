using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands
{
    /// <summary>
    /// Provides common helper functions for commands.
    /// </summary>
    public static class CommandHelper
    {
        /// <summary>
        /// Returns the <see cref="DBAccount"/> bound to the provided <see cref="NetClient"/>.
        /// </summary>
        public static DBAccount GetClientAccount(NetClient client)
        {
            if (client == null)
                return null;

            if (client.FrontendClient is not IDBAccountOwner accountOwner)
                return null;

            return accountOwner.Account;
        }

        /// <summary>
        /// Searches for a <see cref="PrototypeId"/> that matches the specified <see cref="BlueprintId"/> and <see cref="string"/> pattern.
        /// Returns <see cref="PrototypeId.Invalid"/> if no or more than one match is found.
        /// </summary>
        public static PrototypeId FindPrototype(BlueprintId blueprintRef, string pattern, IFrontendClient client)
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
