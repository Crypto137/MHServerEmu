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

        // Wrap ChatHelper calls here to replace them with ChatManager later

        /// <summary>
        /// Sends the specified text as a metagame chat message to the provided <see cref="NetClient"/>.
        /// </summary>
        /// <remarks>
        /// The in-game chat window does not handle well messages longer than 25-30 lines (~40 characters per line).
        /// If you need to send a long message, use SendMetagameMessages() or SendMetagameMessageSplit().
        /// </remarks>
        public static void SendMessage(NetClient client, string text, bool showSender = true)
        {
            ChatHelper.SendMetagameMessage(client.FrontendClient, text, showSender);
        }

        /// <summary>
        /// Sends the specified collection of texts as metagame chat messages to the provided <see cref="NetClient"/>.
        /// </summary>
        public static void SendMessages(NetClient client, IEnumerable<string> texts, bool showSender = true)
        {
            ChatHelper.SendMetagameMessages(client.FrontendClient, texts, showSender);
        }

        /// <summary>
        /// Splits the specified text at line breaks and sends it as a collection of metagame chat messages to the provided <see cref="NetClient"/>.
        /// </summary>
        public static void SendMessageSplit(NetClient client, string text, bool showSender = true)
        {
            ChatHelper.SendMetagameMessageSplit(client.FrontendClient, text, showSender);
        }

        /// <summary>
        /// Searches for a <see cref="PrototypeId"/> that matches the specified <see cref="BlueprintId"/> and <see cref="string"/> pattern.
        /// Returns <see cref="PrototypeId.Invalid"/> if no or more than one match is found.
        /// </summary>
        public static PrototypeId FindPrototype(BlueprintId blueprintRef, string pattern, NetClient client)
        {
            const int MaxMatches = 10;

            IFrontendClient frontendClient = client.FrontendClient;

            IEnumerable<PrototypeId> matches = GameDatabase.SearchPrototypes(pattern,
                DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprintRef);

            // Not enough
            if (matches.Any() == false)
            {
                ChatHelper.SendMetagameMessage(frontendClient, $"Failed to find any {((PrototypeId)blueprintRef).GetNameFormatted()} prototypes containing {pattern}.");
                return PrototypeId.Invalid;
            }

            // Too many
            int numMatches = matches.Count();
            if (numMatches > 1)
            {
                var matchNames = matches.Select(match => GameDatabase.GetPrototypeName(match));

                if (numMatches <= MaxMatches)
                {
                    ChatHelper.SendMetagameMessage(frontendClient, $"Found multiple matches for {pattern}:");
                    ChatHelper.SendMetagameMessages(frontendClient, matchNames, false);
                }
                else
                {
                    ChatHelper.SendMetagameMessage(frontendClient, $"Found over {MaxMatches} matches for {pattern}, here are the first {MaxMatches}:");
                    ChatHelper.SendMetagameMessages(frontendClient, matchNames.Take(MaxMatches), false);
                }

                return PrototypeId.Invalid;
            }

            return matches.First();
        }
    }
}
