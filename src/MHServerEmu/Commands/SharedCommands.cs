using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Commands
{
    [CommandGroup("lookup", "Searches for data id by name.\nUsage: lookup [costume|region|blueprint|assettype] [pattern]", AccountUserLevel.User)]
    public class LookupCommands : CommandGroup
    {
        [Command("costume", "Searches prototypes that use the costume blueprint.\nUsage: lookup costume [pattern]", AccountUserLevel.User)]
        public string Costume(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup costume' to get help.";

            // Find matches for the given pattern
            return LookupPrototypes(@params[0], (BlueprintId)10774581141289766864, client);     // Entity/Items/Costumes/Costume.blueprint
        }

        [Command("region", "Searches prototypes that use the region blueprint.\nUsage: lookup region [pattern]", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup region' to get help.";

            // Find matches for the given pattern
            return LookupPrototypes(@params[0], (BlueprintId)1677652504589371837, client);      // Regions/Region.blueprint
        }

        [Command("blueprint", "Searches blueprints.\nUsage: lookup blueprint [pattern]", AccountUserLevel.User)]
        public string Blueprint(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup blueprint' to get help.";

            // Find matches for the given pattern
            var matches = GameDatabase.SearchBlueprints(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetBlueprintName(match))), client);
        }

        [Command("assettype", "Searches asset types.\nUsage: lookup assettype [pattern]", AccountUserLevel.User)]
        public string AssetType(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup assettype' to get help.";

            var matches = GameDatabase.SearchAssetTypes(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetAssetTypeName(match))), client);
        }

        [Command("asset", "Searches assets.\nUsage: lookup asset [pattern]", AccountUserLevel.User)]
        public string Asset(string[] @params, FrontendClient client)
        {
            if (@params == null) return Fallback();
            if (@params.Length == 0) return "Invalid arguments. Type 'help lookup asset' to get help.";

            var matches = GameDatabase.SearchAssets(@params[0], DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive);
            return OutputLookupMatches(matches.Select(match => ((ulong)match,
                $"{GameDatabase.GetAssetName(match)} ({GameDatabase.GetAssetTypeName(GameDatabase.DataDirectory.AssetDirectory.GetAssetTypeRef(match))})")),
                client);
        }

        private static string LookupPrototypes(string pattern, BlueprintId blueprint, FrontendClient client)
        {
            var matches = GameDatabase.SearchPrototypes(pattern, DataFileSearchFlags.SortMatchesByName | DataFileSearchFlags.CaseInsensitive, blueprint);
            return OutputLookupMatches(matches.Select(match => ((ulong)match, GameDatabase.GetPrototypeName(match))), client);
        }

        private static string OutputLookupMatches(IEnumerable<(ulong, string)> matches, FrontendClient client)
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
            ChatHelper.SendMetagameMessages(client, outputList);

            return string.Empty;
        }
    }

    [CommandGroup("debug", "Debug commands for development.", AccountUserLevel.User)]
    public class DebugCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("test", "Runs test code.", AccountUserLevel.Admin)]
        public string Test(string[] @params, FrontendClient client)
        {
            return string.Empty;
        }

        [Command("cell", "Shows current cell.", AccountUserLevel.User)]
        public string Cell(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);

            return $"Current cell: {playerConnection.AOI.Region.GetCellAtPosition(playerConnection.LastPosition).PrototypeName}";
        }

        [Command("seed", "Shows current seed.", AccountUserLevel.User)]
        public string Seed(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);

            return $"Current seed: {playerConnection.AOI.Region.RandomSeed}";
        }

        [Command("area", "Shows current area.", AccountUserLevel.User)]
        public string Area(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);

            return $"Current area: {playerConnection.AOI.Region.GetCellAtPosition(playerConnection.LastPosition).Area.PrototypeName}";
        }

        [Command("region", "Shows current region.", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var connection = game.NetworkManager.GetPlayerConnection(client);

            return $"Current region: {connection.AOI.Region.PrototypeName}";
        }

        [Command("isblocked", "Usage: debug isblocked [EntityId1] [EntityId2]", AccountUserLevel.User)]

        public string isblocked(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help debug isblocked' to get help.";

            if (ulong.TryParse(@params[0], out ulong entityId1) == false)
                return $"Failed to parse EntityId1 {@params[0]}";

            if (ulong.TryParse(@params[1], out ulong entityId2) == false)
                return $"Failed to parse EntityId2 {@params[1]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var manager = game.EntityManager;

            var entity = manager.GetEntityById(entityId1);
            if (entity is not WorldEntity entity1) return $"No entity found for {entityId1}";

            entity = manager.GetEntityById(entityId2);
            if (entity is not WorldEntity entity2) return $"No entity found for {entityId2}";

            Bounds bounds = entity1.Bounds;
            bool isBlocked = Games.Regions.Region.IsBoundsBlockedByEntity(bounds, entity2, BlockingCheckFlags.CheckSpawns);
            return $"Entities\n [{entity1.PrototypeName}]\n [{entity2.PrototypeName}]\nIsBlocked: {isBlocked}";
        }

        [Command("near", "Usage: debug near [radius]. Default radius 100.", AccountUserLevel.User)]
        public string Near(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var playerConnection = game.NetworkManager.GetPlayerConnection(client);

            if ((@params?.Length > 0 && int.TryParse(@params[0], out int radius)) == false)
                radius = 100;   // Default to 100 if no radius is specified

            Sphere near = new(playerConnection.LastPosition, radius);

            List<string> entities = new();
            foreach (var worldEntity in playerConnection.AOI.Region.IterateEntitiesInVolume(near, new()))
            {
                string name = worldEntity.PrototypeName;
                ulong entityId = worldEntity.Id;
                string status = string.Empty;
                if (playerConnection.AOI.EntityLoaded(entityId) == false) status += "[H]";
                if (worldEntity is Transition) status += "[T]";
                if (worldEntity.WorldEntityPrototype.VisibleByDefault == false) status += "[Inv]";
                entities.Add($"[E][{entityId}] {name} {status}");
            }

            foreach (var reservation in playerConnection.AOI.Region.SpawnMarkerRegistry.IterateReservationsInVolume(near))
            {
                string name = GameDatabase.GetFormattedPrototypeName(reservation.MarkerRef);
                int markerId = reservation.GetPid();
                string status = $"[{reservation.Type.ToString()[0]}][{reservation.State.ToString()[0]}]";
                entities.Add($"[M][{markerId}] {name} {status}");
            }

            if (entities.Count == 0)
                return "No objects found.";

            ChatHelper.SendMetagameMessage(client, $"Found for R={radius}:");
            ChatHelper.SendMetagameMessages(client, entities, false);
            return string.Empty;
        }

        [Command("marker", "Displays information about the specified marker.\nUsage: debug marker [MarkerId]", AccountUserLevel.User)]
        public string marker(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help debug marker' to get help.";

            if (int.TryParse(@params[0], out int markerId) == false)
                return $"Failed to parse MarkerId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var connection = game.NetworkManager.GetPlayerConnection(client);

            var reservation = connection.AOI.Region.SpawnMarkerRegistry.GetReservationByPid(markerId);
            if (reservation == null) return "No marker found.";

            ChatHelper.SendMetagameMessage(client, $"Marker[{markerId}]: {GameDatabase.GetFormattedPrototypeName(reservation.MarkerRef)}");
            ChatHelper.SendMetagameMessages(client, reservation.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries), false);
            return string.Empty;
        }

        [Command("entity", "Displays information about the specified entity.\nUsage: debug entity [EntityId]", AccountUserLevel.User)]
        public string entity(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help debug entity' to get help.";

            if (ulong.TryParse(@params[0], out ulong entityId) == false)
                return $"Failed to parse EntityId {@params[0]}";

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            var game = playerManager.GetGameByPlayer(client);
            var entity = game.EntityManager.GetEntityById(entityId);
            if (entity == null) return "No entity found.";

            ChatHelper.SendMetagameMessage(client, $"Entity[{entityId}]: {GameDatabase.GetFormattedPrototypeName(entity.BaseData.PrototypeId)}");
            ChatHelper.SendMetagameMessages(client, entity.Properties.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries), false);
            if (entity is WorldEntity worldEntity)
            {
                ChatHelper.SendMetagameMessages(client, worldEntity.Bounds.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries), false);
                ChatHelper.SendMetagameMessages(client, worldEntity.PowerCollectionToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries), false);
            }
            return string.Empty;
        }
    }
}
