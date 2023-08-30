using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("lookup", "Searches for prototype id by name.\nUsage: lookup [costume] [pattern]")]
    public class LookupCommands : CommandGroup
    {
        [Command("costume", "Usage: lookup costume [pattern]")]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();

            List<DataDirectoryPrototypeEntry> matches = new();

            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

            string pattern = @params[0].ToLower();

            foreach (DataDirectoryPrototypeEntry entry in GameDatabase.Calligraphy.PrototypeDirectory.Entries)
            {
                if (entry.Name.Contains("Entity\\Items\\Costumes\\Prototypes\\") && entry.Name.ToLower().Contains(pattern))
                    matches.Add(entry);
            }

            return matches.Aggregate(matches.Count >= 1 ? "Costume Matches:\n" : "No match found.",
                (current, match) => $"{current}[{match.Id1}] {Path.GetRelativePath("Entity\\Items\\Costumes\\Prototypes\\", match.Name)}\n");
        }
    }
}
