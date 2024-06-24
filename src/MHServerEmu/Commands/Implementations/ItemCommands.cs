using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("item", "Provides commands for creating items.")]
    public class ItemCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("drop", "Creates and drops the specified item from the current avatar. Optionally specify count.\nUsage: item drop [pattern] [count]")]
        public string Drop(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item drop' to get help.";

            PrototypeId itemProtoRef = FindItem(@params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            if (@params.Length == 1 || int.TryParse(@params[1], out int count) == false)
                count = 1;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            LootGenerator lootGenerator = playerConnection.Game.LootGenerator;
            
            for (int i = 0; i < count; i++)
            {
                var item = lootGenerator.DropItem(avatar, itemProtoRef, 100f);
                Logger.Debug($"DropItem(): {item} from {avatar}");
            }

            return string.Empty;
        }

        [Command("give", "Creates and drops the specified item to the current player.\nUsage: item give [pattern]")]
        public string Give(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item give' to get help.";

            PrototypeId itemProtoRef = FindItem(@params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            LootGenerator lootGenerator = playerConnection.Game.LootGenerator;
            var item = lootGenerator.GiveItem(player, itemProtoRef);
            Logger.Debug($"GiveItem(): {item} to {player}");

            return string.Empty;
        }

        private PrototypeId FindItem(string pattern, FrontendClient client)
        {
            const int MaxMatches = 10;

            IEnumerable<PrototypeId> matches = GameDatabase.SearchPrototypes(pattern,
                DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, HardcodedBlueprints.Item);

            // Not enough
            if (matches.Any() == false)
            {
                ChatHelper.SendMetagameMessage(client, $"Failed to find any items containing {pattern}.");
                return PrototypeId.Invalid;
            }

            // Too many
            int numMatches = matches.Count();
            if (numMatches > 1)
            {
                var matchNames = matches.Select(match => GameDatabase.GetPrototypeName(match));

                if (numMatches <= MaxMatches)
                {
                    ChatHelper.SendMetagameMessage(client, $"Found multiple matches for {pattern}:");
                    ChatHelper.SendMetagameMessages(client, matchNames, false);
                }
                else
                {
                    ChatHelper.SendMetagameMessage(client, $"Found over {MaxMatches} matches for {pattern}, here are the first {MaxMatches}:");
                    ChatHelper.SendMetagameMessages(client, matchNames.Take(MaxMatches), false);
                }

                return PrototypeId.Invalid;
            }

            return matches.First();
        }
    }
}
