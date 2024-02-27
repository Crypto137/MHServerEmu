using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Generators;
using MHServerEmu.Grouping;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Accounts;

namespace MHServerEmu.Common.Commands
{
    [CommandGroup("server", "Allows you to interact with the server.", AccountUserLevel.User)]
    public class ServerCommands : CommandGroup
    {
        [Command("status", "Usage: server status", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            return ServerManager.Instance.GetServerStatus();
        }

        [Command("shutdown", "Usage: server shutdown", AccountUserLevel.Admin)]
        public string Shutdown(string[] @params, FrontendClient client)
        {
            Program.Shutdown();
            return string.Empty;
        }
    }

    [CommandGroup("client", "Allows you to interact with clients.", AccountUserLevel.Admin)]
    public class ClientCommands : CommandGroup
    {
        [Command("info", "Usage: client info [sessionId]", AccountUserLevel.Admin)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client info' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            if (ServerManager.Instance.PlayerManagerService.TryGetSession(sessionId, out ClientSession session) == false)
                return $"SessionId {sessionId} not found";

            return session.ToString();
        }

        [Command("kick", "Usage: client kick [playerName]", AccountUserLevel.Moderator)]
        public string Kick(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help client kick' to get help.";

            if (ServerManager.Instance.GroupingManagerService.TryGetPlayerByName(@params[0], out FrontendClient target) == false)
                return $"Player {@params[0]} not found.";

            target.Connection.Disconnect();
            return $"Kicked {target.Session.Account}.";
        }

        [Command("send", "Usage: client send [sessionId] [messageName] [messageContent]", AccountUserLevel.Admin)]
        public string Send(string[] @params, FrontendClient client)
        {
            if (@params == null || @params.Length < 3) return "Invalid arguments. Type 'help client send' to get help.";

            if (ulong.TryParse(@params[0], out ulong sessionId) == false)
                return $"Failed to parse sessionId {@params[0]}";

            if (ServerManager.Instance.PlayerManagerService.TryGetClient(sessionId, out FrontendClient target) == false)
                return $"Client for sessionId {sessionId} not found";

            switch (@params[1].ToLower())
            {
                case "chatnormalmessage":
                    string message = @params[2];
                    for (int i = 3; i < @params.Length; i++)
                        message += " " + @params[i];

                    var chatMessage = ChatNormalMessage.CreateBuilder()
                        .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                        .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                        .SetTheMessage(ChatMessage.CreateBuilder().SetBody(message))
                        .SetPrestigeLevel(6)
                        .Build();

                    target.SendMessage(2, new(chatMessage));
                    break;

                default:
                    return $"Unsupported message {@params[1]}";
            }

            return string.Empty;
        }
    }

    [CommandGroup("packet", "Provides commands to interact with packet files.", AccountUserLevel.Admin)]
    public class PacketCommands : CommandGroup
    {
        [Command("parse", "Parses messages from all packets\nUsage: packet parse", AccountUserLevel.Admin)]
        public string Extract(string[] @params, FrontendClient client)
        {
            if (client != null)
                return "You can only invoke this command from the server console.";

            PacketHelper.ParseServerMessagesFromAllPacketFiles();

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
            return $"Current cell: {client.AOI.Region.GetCellAtPosition(client.LastPosition).PrototypeName}";
        }

        [Command("seed", "Shows current seed.", AccountUserLevel.User)]
        public string Seed(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            return $"Current seed: {client.AOI.Region.RandomSeed}";
        }

        [Command("area", "Shows current area.", AccountUserLevel.User)]
        public string Area(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            return $"Current area: {client.AOI.Region.GetCellAtPosition(client.LastPosition).Area.PrototypeName}";
        }

        [Command("near", "Usage: debug near [radius]. Default radius 100.", AccountUserLevel.User)]
        public string Near(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            if ((@params?.Length > 0 && int.TryParse(@params[0], out int radius)) == false)
                radius = 100;   // Default to 100 if no radius is specified

            Sphere near = new(client.LastPosition, radius);
            EntityRegionSPContext context = new() { Flags = EntityRegionSPContextFlags.ActivePartition | EntityRegionSPContextFlags.StaticPartition };

            List<string> entities = new();
            foreach (var worldEntity in client.AOI.Region.IterateEntitiesInVolume(near, context))
            {
                string name = GameDatabase.GetFormattedPrototypeName(worldEntity.BaseData.PrototypeId);
                ulong entityId = worldEntity.BaseData.EntityId;
                string status = string.Empty;
                if (client.AOI.EntityLoaded(entityId) == false) status += "[H]";
                if (worldEntity is Transition) status += "[T]";
                if (worldEntity.WorldEntityPrototype.VisibleByDefault == false) status += "[Invis]";
                entities.Add($"[{entityId}] {name} {status}");
            }

            if (entities.Count == 0)
                return "No entities found.";

            ChatHelper.SendMetagameMessage(client, $"Found for R={radius}:");
            ChatHelper.SendMetagameMessages(client, entities);
            return string.Empty;
        }

        [Command("entity", "Displays information about the specified entity.\nUsage: debug entity [EntityId]", AccountUserLevel.User)]
        public string entity(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params == null || @params.Length == 0) return "Invalid arguments. Type 'help debug entity' to get help.";

            if (ulong.TryParse(@params[0], out ulong entityId) == false)
                return $"Failed to parse EntityId {@params[0]}";

            var entity = client.CurrentGame.EntityManager.GetEntityById(entityId);
            if (entity == null) return "No entity found.";

            ChatHelper.SendMetagameMessage(client, $"Entity[{entityId}]: {GameDatabase.GetFormattedPrototypeName(entity.BaseData.PrototypeId)}");
            ChatHelper.SendMetagameMessages(client, entity.Properties.ToString().Split('\n'));
            return string.Empty;
        }
    }
}
