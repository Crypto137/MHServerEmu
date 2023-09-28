using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.Networking;

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
            List<DataDirectoryPrototypeRecord> matchList = new();
            string pattern = @params[0].ToLower();

            foreach (DataDirectoryPrototypeRecord record in GameDatabase.Calligraphy.PrototypeDirectory.Records)
            {
                if (record.FilePath.Contains("Entity/Items/Costumes/Prototypes/") && record.FilePath.ToLower().Contains(pattern))
                    matchList.Add(record);
            }

            // Output
            return OutputPrototypeLookup(matchList, "Entity/Items/Costumes/Prototypes/", client);
        }

        [Command("region", "Usage: lookup region [pattern]", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup region' to get help.";

            // Find matches for the given pattern
            List<DataDirectoryPrototypeRecord> matchList = new();
            string pattern = @params[0].ToLower();

            foreach (DataDirectoryPrototypeRecord record in GameDatabase.Calligraphy.PrototypeDirectory.Records)
            {
                if (record.FilePath.Contains("Regions/"))
                {
                    string fileName = Path.GetFileName(record.FilePath);
                    if (fileName.Contains("Region") && Path.GetExtension(fileName) == ".prototype" && fileName.ToLower().Contains(pattern))
                        matchList.Add(record);
                }
            }

            // Output
            return OutputPrototypeLookup(matchList, "Regions/", client);
        }

        private static string OutputPrototypeLookup(List<DataDirectoryPrototypeRecord> matchList, string rootDirectory, FrontendClient client)
        {
            if (matchList.Count > 0)
            {
                if (client == null)
                {
                    // Output as a single string with line breaks if the command was invoked from the console
                    return matchList.Aggregate("Lookup Matches:\n",
                        (current, match) => $"{current}[{match.Id}] {Path.GetRelativePath(rootDirectory, match.FilePath)}\n");
                }
                else
                {
                    // Output as a list of chat messages if the command was invoked from the in-game chat
                    // This is because the chat window doesn't handle individual messages with too many lines well (e.g. when the lookup pattern is not specific enough)
                    List<string> outputList = new() { "Lookup Matches:" };
                    outputList.AddRange(matchList.Select(match => $"[{match.Id}] {Path.GetRelativePath(rootDirectory, match.FilePath)}"));
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
