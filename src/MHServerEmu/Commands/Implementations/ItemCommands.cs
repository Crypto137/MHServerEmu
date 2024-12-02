using System.Diagnostics;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("item", "Provides commands for creating items.")]
    public class ItemCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("drop", "Creates and drops the specified item from the current avatar. Optionally specify count.\nUsage: item drop [pattern] [count]", AccountUserLevel.Admin)]
        public string Drop(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item drop' to get help.";

            PrototypeId itemProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Item, @params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            if (@params.Length == 1 || int.TryParse(@params[1], out int count) == false)
                count = 1;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;
            Avatar avatar = player.CurrentAvatar;

            LootManager lootManager = playerConnection.Game.LootManager;
            
            for (int i = 0; i < count; i++)
            {
                lootManager.SpawnItem(itemProtoRef, LootContext.Drop, player, avatar);
                Logger.Debug($"DropItem(): {itemProtoRef.GetName()} from {avatar}");
            }

            return string.Empty;
        }

        [Command("give", "Creates and drops the specified item to the current player.\nUsage: item give [pattern]", AccountUserLevel.Admin)]
        public string Give(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item give' to get help.";

            PrototypeId itemProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.Item, @params[0], client);
            if (itemProtoRef == PrototypeId.Invalid) return string.Empty;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            LootManager lootGenerator = playerConnection.Game.LootManager;
            lootGenerator.GiveItem(itemProtoRef, LootContext.Drop, player);
            Logger.Debug($"GiveItem(): {itemProtoRef.GetName()} to {player}");

            return string.Empty;
        }

        [Command("destroyindestructible", "Destroys indestructible items contained in the player's general inventory.\nUsage: item destroyindestructible")]
        public string DestroyIndestructible(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;
            Inventory general = player.GetInventory(InventoryConvenienceLabel.General);

            List<Item> indestructibleItemList = new();
            foreach (var entry in general)
            {
                Item item = player.Game.EntityManager.GetEntity<Item>(entry.Id);
                if (item == null) continue;

                if (item.ItemPrototype.CanBeDestroyed == false)
                    indestructibleItemList.Add(item);
            }

            foreach (Item item in indestructibleItemList)
                item.Destroy();

            return $"Destroyed {indestructibleItemList.Count} indestructible items.";
        }

        [Command("roll", "Rolls a loot table.\nUsage: item roll [pattern]", AccountUserLevel.Admin)]
        public string RollLootTable(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help item roll' to get help.";

            PrototypeId lootTableProtoRef = CommandHelper.FindPrototype(HardcodedBlueprints.LootTable, @params[0], client);
            if (lootTableProtoRef == PrototypeId.Invalid) return string.Empty;

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            player.Game.LootManager.TestLootTable(lootTableProtoRef, player);

            return $"Finished rolling {lootTableProtoRef.GetName()}, see the server console for results.";
        }

        [Command("rollall", "Rolls all loot tables.\nUsage: item rollall", AccountUserLevel.Admin)]
        public string RollAllLootTables(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            int numLootTables = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();

            foreach (PrototypeId lootTableProtoRef in DataDirectory.Instance.IteratePrototypesInHierarchy<LootTablePrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                player.Game.LootManager.TestLootTable(lootTableProtoRef, player);
                numLootTables++;
            }

            stopwatch.Stop();

            return $"Finished rolling {numLootTables} loot tables in {stopwatch.Elapsed.TotalMilliseconds} ms, see the server console for results.";
        }
    }
}
