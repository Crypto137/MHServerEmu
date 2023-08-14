using MHServerEmu.GameServer.Data;
using MHServerEmu.Networking;

namespace MHServerEmu.Common.Commands
{
    public class SharedCommands
    {
        [CommandGroup("lookup", "Searches for prototype id by name.\nUsage: lookup [costume] [pattern]")]
        public class ServerCommands : CommandGroup
        {
            [Command("costume", "Usage: lookup costume [pattern]")]
            public string Costume(string[] @params, FrontendClient client)
            {
                if (@params == null) return Fallback();

                List<KeyValuePair<ulong, string>> matches = new();

                if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

                string pattern = @params[0].ToLower();

                foreach (var kvp in Database.HashMapDict[HashMapType.Prototype].ForwardDict)
                {
                    if (kvp.Value.Contains("Entity/Items/Costumes/Prototypes/") && kvp.Value.ToLower().Contains(pattern))
                        matches.Add(kvp);
                }

                return matches.Aggregate(matches.Count >= 1 ? "Costume Matches:\n" : "No match found.",
                    (current, match) => $"{current}[{match.Key}] {Path.GetRelativePath("Entity/Items/Costumes/Prototypes/", match.Value)}\n");
            }
        }
    }
}
