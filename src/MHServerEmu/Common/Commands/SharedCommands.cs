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

            List<DataDirectoryPrototypeEntry> matches = new();

            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

            string pattern = @params[0].ToLower();

            foreach (DataDirectoryPrototypeEntry entry in GameDatabase.Calligraphy.PrototypeDirectory.Entries)
            {
                if (entry.FilePath.Contains("Entity/Items/Costumes/Prototypes/") && entry.FilePath.ToLower().Contains(pattern))
                    matches.Add(entry);
            }

            return matches.Aggregate(matches.Count >= 1 ? "Costume Matches:\n" : "No match found.",
                (current, match) => $"{current}[{match.Id}] {Path.GetRelativePath("Entity/Items/Costumes/Prototypes/", match.FilePath)}\n");
        }
    }
}
