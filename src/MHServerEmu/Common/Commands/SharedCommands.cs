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
            var matchList = GameDatabase.PrototypeRefManager.LookupCostume(@params[0].ToLower());
            return OutputPrototypeLookup(matchList, "Entity/Items/Costumes/Prototypes/", client);
        }

        [Command("region", "Usage: lookup region [pattern]", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup region' to get help.";

            // Find matches for the given pattern
            var matchList = GameDatabase.PrototypeRefManager.LookupRegion(@params[0].ToLower());
            return OutputPrototypeLookup(matchList, "Regions/", client);
        }

        private static string OutputPrototypeLookup(List<KeyValuePair<ulong, string>> matchList, string rootDirectory, FrontendClient client)
        {
            if (matchList.Count > 0)
            {
                if (client == null)
                {
                    // Output as a single string with line breaks if the command was invoked from the console
                    return matchList.Aggregate("Lookup Matches:\n",
                        (current, match) => $"{current}[{match.Key}] {Path.GetRelativePath(rootDirectory, match.Value)}\n");
                }
                else
                {
                    // Output as a list of chat messages if the command was invoked from the in-game chat
                    // This is because the chat window doesn't handle individual messages with too many lines well (e.g. when the lookup pattern is not specific enough)
                    List<string> outputList = new() { "Lookup Matches:" };
                    outputList.AddRange(matchList.Select(match => $"[{match.Key}] {Path.GetRelativePath(rootDirectory, match.Value)}"));
                    GroupingManagerService.SendMetagameChatMessages(client, outputList);
                    return string.Empty;
                }
            }
            else
            {
                return "No match found.";
            }
        }
    }
}
