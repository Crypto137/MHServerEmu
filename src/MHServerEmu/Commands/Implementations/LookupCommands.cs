using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("lookup", "Searches for data id by name.\nUsage: lookup [costume|region|blueprint|assettype|asset] [pattern]")]
    [CommandGroupUserLevel(AccountUserLevel.Admin)]
    public class LookupCommands : CommandGroup
    {
        [Command("power", "Searches prototypes that use the power blueprint.\nUsage: lookup power [pattern]")]
        [CommandParamCount(1)]
        public string Power(string[] @params, NetClient client)
        {
            // Find matches for the given pattern
            return LookupPrototypes(@params[0], HardcodedBlueprints.Power, client);
        }

        [Command("item", "Searches prototypes that use the item blueprint.\nUsage: lookup item [pattern]")]
        [CommandParamCount(1)]
        public string Item(string[] @params, NetClient client)
        {
            // Find matches for the given pattern
            return LookupPrototypes(@params[0], HardcodedBlueprints.Item, client);
        }

        [Command("costume", "Searches prototypes that use the costume blueprint.\nUsage: lookup costume [pattern]")]
        [CommandParamCount(1)]
        public string Costume(string[] @params, NetClient client)
        {
            // Find matches for the given pattern
            return LookupPrototypes(@params[0], HardcodedBlueprints.Costume, client);
        }

        [Command("region", "Searches prototypes that use the region blueprint.\nUsage: lookup region [pattern]")]
        [CommandParamCount(1)]
        public string Region(string[] @params, NetClient client)
        {
            // Find matches for the given pattern
            return LookupPrototypes(@params[0], HardcodedBlueprints.Region, client);      // Regions/Region.blueprint
        }

        [Command("blueprint", "Searches blueprints.\nUsage: lookup blueprint [pattern]")]
        [CommandParamCount(1)]
        public string Blueprint(string[] @params, NetClient client)
        {
            // Find matches for the given pattern
            var matches = GameDatabase.SearchBlueprints(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetBlueprintName(match))), client);
        }

        [Command("assettype", "Searches asset types.\nUsage: lookup assettype [pattern]")]
        [CommandParamCount(1)]
        public string AssetType(string[] @params, NetClient client)
        {
            var matches = GameDatabase.SearchAssetTypes(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetAssetTypeName(match))), client);
        }

        [Command("asset", "Searches assets.\nUsage: lookup asset [pattern]")]
        [CommandParamCount(1)]
        public string Asset(string[] @params, NetClient client)
        {
            var matches = GameDatabase.SearchAssets(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match,
                $"{GameDatabase.GetAssetName(match)} ({GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeRef(match))})")),
                client);
        }

        private static string LookupPrototypes(string pattern, BlueprintId blueprint, NetClient client)
        {
            var matches = GameDatabase.SearchPrototypes(pattern, DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprint);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetPrototypeName(match))), client);
        }

        private static string OutputLookupMatches(IEnumerable<(ulong, string)> matches, NetClient client)
        {
            if (matches.Any() == false)
                return "No matches found.";

            if (client == null)
            {
                // Output as a single string with line breaks if the command was invoked from the console
                return matches.Aggregate("Lookup Matches:\n",
                    (current, match) => $"{current}[{match.Item1}] {match.Item2}\n");
            }

            // Output as a list of chat messages if the command was invoked from the in-game chat.
            // This is because the chat window doesn't handle individual messages with too many lines well (e.g. when the lookup pattern is not specific enough).
            // Also we do not add a space between prototype id and name to prevent the client from adding a line break there.
            List<string> outputList = new() { "Lookup Matches:" };
            outputList.AddRange(matches.Select(match => $"[{match.Item1}]{match.Item2}"));
            CommandHelper.SendMessages(client, outputList);

            return string.Empty;
        }
    }
}
