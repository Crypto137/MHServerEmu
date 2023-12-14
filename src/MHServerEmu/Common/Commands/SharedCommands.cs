using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("lookup", "Searches for prototype id by name.\nUsage: lookup [costume|region] [pattern]", AccountUserLevel.User)]
    public class LookupCommands : CommandGroup
    {
        [Command("costume", "Usage: lookup costume [pattern]", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

            // Find matches for the given pattern
            BlueprintId blueprint = (BlueprintId)10774581141289766864;   // Entity/Items/Costumes/Costume.blueprint
            return LookupPrototypes(@params[0], blueprint, client);
        }

        [Command("region", "Usage: lookup region [pattern]", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup region' to get help.";

            // Find matches for the given pattern
            BlueprintId blueprint = (BlueprintId)1677652504589371837;   // Regions/Region.blueprint
            return LookupPrototypes(@params[0], blueprint, client);
        }

        private static string LookupPrototypes(string pattern, BlueprintId blueprint, FrontendClient client)
        {
            // Search game database for the given pattern
            var matches = GameDatabase.SearchPrototypes(pattern, DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprint);

            if (matches.Any() == false)
                return "No matches found.";

            if (client == null)
            {
                // Output as a single string with line breaks if the command was invoked from the console
                return matches.Aggregate("Lookup Matches:\n",
                    (current, match) => $"{current}[{match}] {GameDatabase.GetPrototypeName(match)}\n");
            }

            // Output as a list of chat messages if the command was invoked from the in-game chat.
            // This is because the chat window doesn't handle individual messages with too many lines well (e.g. when the lookup pattern is not specific enough).
            // Also we do not add a space between prototype id and name to prevent the client from adding a line break there.
            List<string> outputList = new() { "Lookup Matches:" };
            outputList.AddRange(matches.Select(match => $"[{match}]{GameDatabase.GetPrototypeName(match)}"));
            ChatHelper.SendMetagameMessages(client, outputList);

            return string.Empty;
        }
    }
}
