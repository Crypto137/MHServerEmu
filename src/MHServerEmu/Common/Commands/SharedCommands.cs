using MHServerEmu.GameServer;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("lookup", "Searches for prototype id by name.\nUsage: lookup [costume] [pattern]", AccountUserLevel.User)]
    public class LookupCommands : CommandGroup
    {
        [Command("costume", "Usage: lookup costume [pattern]", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

            // Find matches for the given pattern
            List<DataDirectoryPrototypeEntry> matches = new();
            string pattern = @params[0].ToLower();

            foreach (DataDirectoryPrototypeEntry entry in GameDatabase.Calligraphy.PrototypeDirectory.Entries)
            {
                if (entry.FilePath.Contains("Entity/Items/Costumes/Prototypes/") && entry.FilePath.ToLower().Contains(pattern))
                    matches.Add(entry);
            }

            // Output
            if (matches.Count > 0)
            {
                if (client == null)     
                {
                    // Output as a single string with line breaks if the command was invoked from the console
                    return matches.Aggregate("Costume Matches:\n",
                        (current, match) => $"{current}[{match.Id}] {Path.GetRelativePath("Entity/Items/Costumes/Prototypes/", match.FilePath)}\n");
                }
                else                    
                {
                    // Output as a list of chat messages if the command was invoked from the in-game chat
                    // This is because the chat window doesn't handle individual messages with too many lines well (e.g. when the lookup pattern is not specific enough)
                    List<string> outputList = new() { "Costume Matches:" };
                    outputList.AddRange(matches.Select(match => $"[{match.Id}] {Path.GetRelativePath("Entity/Items/Costumes/Prototypes/", match.FilePath)}"));
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
