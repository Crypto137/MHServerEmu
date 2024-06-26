using System.Text;
using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Grouping;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("debug", "Debug commands for development.", AccountUserLevel.User)]
    public class DebugCommands : CommandGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [Command("test", "Runs test code.", AccountUserLevel.Admin)]
        public string Test(string[] @params, FrontendClient client)
        {
            return string.Empty;
        }

        [Command("forcegc", "Requests the garbage collector to reclaim unused server memory.", AccountUserLevel.Admin)]
        public string ForceGC(string[] @params, FrontendClient client)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            return "Manual garbage collection successfully requested.";
        }

        [Command("cell", "Shows current cell.", AccountUserLevel.User)]
        public string Cell(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current cell: {playerConnection.AOI.Region.GetCellAtPosition(playerConnection.LastPosition).PrototypeName}";
        }

        [Command("seed", "Shows current seed.", AccountUserLevel.User)]
        public string Seed(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current seed: {playerConnection.AOI.Region.RandomSeed}";
        }

        [Command("area", "Shows current area.", AccountUserLevel.User)]
        public string Area(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current area: {playerConnection.AOI.Region.GetCellAtPosition(playerConnection.LastPosition).Area.PrototypeName}";
        }

        [Command("region", "Shows current region.", AccountUserLevel.User)]
        public string Region(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            return $"Current region: {playerConnection.AOI.Region.PrototypeName}";
        }
        

        [Command("navi2obj", "Usage: debug navi2obj [PathFlags].\n Default PathFlags is Walk, can be [None|Fly|Power|Sight].", AccountUserLevel.User)]
        public string Navi2Obj(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var region = playerConnection.AOI.Region;

            if ((@params.Length > 0 && Enum.TryParse(@params[0], out PathFlags flags)) == false)
                flags = PathFlags.Walk;   // Default Walk

            string filename = $"{region.PrototypeName}[{flags}].obj";
            string obj = region.NaviMesh.NaviCdt.MeshToObj(flags);
            FileHelper.SaveTextFileToRoot(filename, obj);
            return $"NaviMesh saved as {filename}";
        }

        [Command("isblocked", "Usage: debug isblocked [EntityId1] [EntityId2]", AccountUserLevel.User)]
        public string IsBlocked(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help debug isblocked' to get help.";

            if (ulong.TryParse(@params[0], out ulong entityId1) == false)
                return $"Failed to parse EntityId1 {@params[0]}";

            if (ulong.TryParse(@params[1], out ulong entityId2) == false)
                return $"Failed to parse EntityId2 {@params[1]}";

            CommandHelper.TryGetGame(client, out Game game);
            var manager = game.EntityManager;

            var entity1 = manager.GetEntity<WorldEntity>(entityId1);
            if (entity1 == null) return $"No entity found for {entityId1}";

            var entity2 = manager.GetEntity<WorldEntity>(entityId2);
            if (entity2 == null) return $"No entity found for {entityId2}";

            Bounds bounds = entity1.Bounds;
            bool isBlocked = Games.Regions.Region.IsBoundsBlockedByEntity(bounds, entity2, BlockingCheckFlags.CheckSpawns);
            return $"Entities\n [{entity1.PrototypeName}]\n [{entity2.PrototypeName}]\nIsBlocked: {isBlocked}";
        }

        [Command("near", "Usage: debug near [radius]. Default radius 100.", AccountUserLevel.User)]
        public string Near(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            if ((@params.Length > 0 && int.TryParse(@params[0], out int radius)) == false)
                radius = 100;   // Default to 100 if no radius is specified

            Sphere near = new(playerConnection.LastPosition, radius);

            List<string> entities = new();
            foreach (var worldEntity in playerConnection.AOI.Region.IterateEntitiesInVolume(near, new()))
            {
                string name = worldEntity.PrototypeName;
                ulong entityId = worldEntity.Id;
                string status = string.Empty;
                if (playerConnection.AOI.InterestedInEntity(entityId) == false) status += "[H]";
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
        public string Marker(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help debug marker' to get help.";

            if (int.TryParse(@params[0], out int markerId) == false)
                return $"Failed to parse MarkerId {@params[0]}";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var reservation = playerConnection.AOI.Region.SpawnMarkerRegistry.GetReservationByPid(markerId);
            if (reservation == null) return "No marker found.";

            ChatHelper.SendMetagameMessage(client, $"Marker[{markerId}]: {GameDatabase.GetFormattedPrototypeName(reservation.MarkerRef)}");
            ChatHelper.SendMetagameMessageSplit(client, reservation.ToString(), false);
            return string.Empty;
        }

        [Command("entity", "Displays information about the specified entity.\nUsage: debug entity [EntityId]", AccountUserLevel.User)]
        public string Entity(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help debug entity' to get help.";

            if (ulong.TryParse(@params[0], out ulong entityId) == false)
                return $"Failed to parse EntityId {@params[0]}";

            CommandHelper.TryGetGame(client, out Game game);

            var entity = game.EntityManager.GetEntity<Entity>(entityId);
            if (entity == null) return "No entity found.";

            ChatHelper.SendMetagameMessage(client, $"Entity[{entityId}]: {GameDatabase.GetFormattedPrototypeName(entity.PrototypeDataRef)}");
            ChatHelper.SendMetagameMessageSplit(client, entity.Properties.ToString(), false);
            if (entity is WorldEntity worldEntity)
            {
                ChatHelper.SendMetagameMessageSplit(client, worldEntity.Bounds.ToString(), false);
                ChatHelper.SendMetagameMessageSplit(client, worldEntity.PowerCollectionToString(), false);
            }
            return string.Empty;
        }

        [Command("powers", "Prints all powers assigned to the current avatar.", AccountUserLevel.User)]
        public string Powers(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            StringBuilder sb = new();
            foreach (var kvp in avatar.PowerCollection)
                sb.AppendLine(kvp.Value.PowerPrototype.ToString());

            if (sb.Length == 0) return $"No powers are assigned to {avatar}.";
            
            ChatHelper.SendMetagameMessage(client, $"Powers assigned to {avatar}:");
            ChatHelper.SendMetagameMessageSplit(client, sb.ToString(), false);

            return string.Empty;
        }

        [Command("scheduletestevent", "Schedules a test event.", AccountUserLevel.Admin)]
        public string ScheduleTestEvent(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetGame(client, out Game game);

            TestEventClass test = new();
            test.ScheduleEvent(game, client, string.Join(' ', @params));

            return $"Test event scheduled";
        }

        private class TestEventClass
        {
            public class TestEvent : CallMethodEventParam2<TestEventClass, FrontendClient, string>
                { protected override CallbackDelegate GetCallback() => (t, p1, p2) => t.EventCallback(p1, p2); }
            private EventPointer<TestEvent> _testEvent = new();

            public void ScheduleEvent(Game game, FrontendClient client, string message)
            {
                game.GameEventScheduler.ScheduleEvent(_testEvent, TimeSpan.FromSeconds(3));
                _testEvent.Get().Initialize(this, client, message);
            }

            public void EventCallback(FrontendClient client, string message)
            {
                ChatHelper.SendMetagameMessage(client, message);
            }
        }
    }
}
