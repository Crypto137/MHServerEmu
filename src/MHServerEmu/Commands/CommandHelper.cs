using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

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
        /// Sends the specified text as a metagame chat message to the provided <see cref="NetClient"/>.
        /// </summary>
        /// <remarks>
        /// The in-game chat window does not handle well messages longer than 25-30 lines (~40 characters per line).
        /// If you need to send a long message, use <see cref="SendMessages"/> or <see cref="SendMessageSplit"/>.
        /// </remarks>
        public static void SendMessage(NetClient client, string text, bool showSender = true)
        {
            ulong playerDbId = ((PlayerConnection)client).PlayerDbId;
            ServiceMessage.GroupingManagerMetagameMessage message = new(playerDbId, text, showSender);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, message);
        }

        /// <summary>
        /// Sends the specified collection of texts as metagame chat messages to the provided <see cref="NetClient"/>.
        /// </summary>
        public static void SendMessages(NetClient client, IEnumerable<string> texts, bool showSender = true)
        {
            if (texts is IList<string> textList)
            {
                int count = textList.Count;
                for (int i = 0; i < count; i++)
                {
                    string text = textList[i];
                    SendMessage(client, text, showSender);
                    showSender = false; // Remove sender from messages after the first one
                }
            }
            else
            {
                foreach (string text in texts)
                {
                    SendMessage(client, text, showSender);
                    showSender = false; // Remove sender from messages after the first one
                }
            }
        }

        /// <summary>
        /// Splits the specified text at line breaks and sends it as a collection of metagame chat messages to the provided <see cref="NetClient"/>.
        /// </summary>
        public static void SendMessageSplit(NetClient client, string text, bool showSender = true)
        {
            SendMessages(client, text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), showSender);
        }

        /// <summary>
        /// Searches for a <see cref="PrototypeId"/> that matches the specified <see cref="BlueprintId"/> and <see cref="string"/> pattern.
        /// Returns <see cref="PrototypeId.Invalid"/> if no or more than one match is found.
        /// </summary>
        public static PrototypeId FindPrototype(BlueprintId blueprintRef, string pattern, NetClient client)
        {
            const int MaxMatches = 10;

            IEnumerable<PrototypeId> matches = GameDatabase.SearchPrototypes(pattern,
                DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprintRef);

            // Not enough
            if (matches.Any() == false)
            {
                SendMessage(client, $"Failed to find any {((PrototypeId)blueprintRef).GetNameFormatted()} prototypes containing {pattern}.");
                return PrototypeId.Invalid;
            }

            // Too many
            int numMatches = matches.Count();
            if (numMatches > 1)
            {
                var matchNames = matches.Select(match => GameDatabase.GetPrototypeName(match));

                if (numMatches <= MaxMatches)
                {
                    SendMessage(client, $"Found multiple matches for {pattern}:");
                    SendMessages(client, matchNames, false);
                }
                else
                {
                    SendMessage(client, $"Found over {MaxMatches} matches for {pattern}, here are the first {MaxMatches}:");
                    SendMessages(client, matchNames.Take(MaxMatches), false);
                }

                return PrototypeId.Invalid;
            }

            return matches.First();
        }
    }
}
