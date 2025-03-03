using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Grouping;
using MHServerEmu.Games;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("Entity", "Provides commands for Entity.")]
    public class EntityCommands : CommandGroup
    {
        [Command("dummy", "Spawn Agent instead of dummy.\nUsage: entity dummy [pattern]", AccountUserLevel.Admin)]
        public string Dummy(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity dummy' to get help.";

            PrototypeId agentRef = CommandHelper.FindPrototype(HardcodedBlueprints.Agent, @params[0], client);
            if (agentRef == PrototypeId.Invalid) return string.Empty;
          
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

            var manager = playerConnection.Game.EntityManager;

            var region = player.GetRegion();
            if (region.PrototypeDataRef != (PrototypeId)12181996598405306634) // TrainingRoomSHIELDRegion
                return "Player is not in Training Room";

            bool found = false;
            Agent dummy = null;
            foreach (var entity in region.Entities)
                if (entity.PrototypeDataRef == (PrototypeId)6534964972476177451)
                {
                    found = true;
                    dummy = entity as Agent;
                }

            if (found == false) return "Dummy is not found";
            dummy.SetDormant(true);

            using (EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>())
            using (PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>())
            {   
                var avatarProp = player.CurrentAvatar.Properties;
                int health = avatarProp[PropertyEnum.HealthMax];
                properties[PropertyEnum.HealthBase] = avatarProp[PropertyEnum.HealthBase];

                settings.EntityRef = agentRef;
                settings.Position = dummy.RegionLocation.Position;
                settings.Orientation = dummy.RegionLocation.Orientation;
                settings.RegionId = region.Id;

                var agent = manager.CreateEntity(settings);
                agent.Properties[PropertyEnum.HealthMax] = health;
                agent.Properties[PropertyEnum.Health] = health;
            }

            return string.Empty;
        }


        [Command("marker", "Displays information about the specified marker.\nUsage: entity marker [MarkerId]", AccountUserLevel.User)]
        public string Marker(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity marker' to get help.";

            if (int.TryParse(@params[0], out int markerId) == false)
                return $"Failed to parse MarkerId {@params[0]}";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);

            var reservation = playerConnection.AOI.Region.SpawnMarkerRegistry.GetReservationByPid(markerId);
            if (reservation == null) return "No marker found.";

            ChatHelper.SendMetagameMessage(client, $"Marker[{markerId}]: {GameDatabase.GetFormattedPrototypeName(reservation.MarkerRef)}");
            ChatHelper.SendMetagameMessageSplit(client, reservation.ToString(), false);
            return string.Empty;
        }


        [Command("info", "Displays information about the specified entity.\nUsage: entity info [EntityId]", AccountUserLevel.User)]
        public string Info(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity info' to get help.";

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
                ChatHelper.SendMetagameMessageSplit(client, worldEntity.ConditionCollectionToString(), false);
            }
            return string.Empty;
        }

        [Command("near", "Usage: entity near [radius]. Default radius 100.", AccountUserLevel.User)]
        public string Near(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            if ((@params.Length > 0 && int.TryParse(@params[0], out int radius)) == false)
                radius = 100;   // Default to 100 if no radius is specified

            Sphere near = new(avatar.RegionLocation.Position, radius);

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

        [Command("isblocked", "Usage: entity isblocked [EntityId1] [EntityId2]", AccountUserLevel.User)]
        public string IsBlocked(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity isblocked' to get help.";

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
    }
}
