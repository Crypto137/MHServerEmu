using MHServerEmu.Commands.Attributes;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Grouping;
using MHServerEmu.Games;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

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
            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(agentRef);

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection);
            Player player = playerConnection.Player;

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

            EntityHelper.CreateAgent(agentProto, player.CurrentAvatar, dummy.RegionLocation.Position, dummy.RegionLocation.Orientation);

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
            bool isBlocked = Region.IsBoundsBlockedByEntity(bounds, entity2, BlockingCheckFlags.CheckSpawns);
            return $"Entities\n [{entity1.PrototypeName}]\n [{entity2.PrototypeName}]\nIsBlocked: {isBlocked}";
        }

        [Command("tp", "Teleports to the first entity present in the region which prototype name contains the string given (ignore the case).\nUsage:\nentity tp modok", AccountUserLevel.Admin)]
        public string Tp(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity tp' to get help.";

            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            Avatar avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return "Avatar not found.";

            if (avatar.Region == null) return "No region found.";

            Entity targetEntity = avatar.Region.Entities.FirstOrDefault(k => k.PrototypeName.ToLower().Contains(@params[0].ToLower()));

            if (targetEntity == null) return $"No entity found with the name {@params[0]}";

            if (targetEntity is not WorldEntity worldEntity)
                return "No world entity found.";

            Vector3 teleportPoint = worldEntity.RegionLocation.Position;
            avatar.ChangeRegionPosition(teleportPoint, null, ChangePositionFlags.Teleport);

            return $"Teleporting to {teleportPoint.ToStringNames()}.";
        }

        [Command("create", "create entity near the avatar based on pattern (ignore the case) and count (default 1).\nUsage:\nentity create bosses/venom 2", AccountUserLevel.Admin)]
        public string Create(string[] @params, FrontendClient client)
        {
            if (client == null) return "You can only invoke this command from the game.";
            if (@params.Length == 0) return "Invalid arguments. Type 'help entity create' to get help.";
            
            CommandHelper.TryGetPlayerConnection(client, out PlayerConnection playerConnection, out Game game);
            if(game == null)
                return "Game not found.";

            Avatar avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return "Avatar not found.";

            Region region = avatar.Region;
            if (region == null) return "No region found.";

            PrototypeId agentRef = CommandHelper.FindPrototype(HardcodedBlueprints.Agent, @params[0], client);
            if (agentRef == PrototypeId.Invalid) return string.Empty;

            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(agentRef);

            int count = 1;
            if (@params.Length == 2)
                int.TryParse(@params[1], out count);

            for (int i = 0; i < count; i++)
            {
                if (EntityHelper.GetSpawnPositionNearAvatar(avatar, region, agentProto.Bounds, 250, out Vector3 positon) == false)
                    return "No space found to spawn the entity";
                var orientation = Orientation.FromDeltaVector(avatar.RegionLocation.Position - positon);
                EntityHelper.CreateAgent(agentProto, avatar, positon, orientation);
            }

            return $"Created!";
        }
    }
}
