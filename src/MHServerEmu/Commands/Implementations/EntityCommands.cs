using MHServerEmu.Commands.Attributes;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Commands.Implementations
{
    [CommandGroup("Entity")]
    [CommandGroupDescription("Entity management commands.")]
    public class EntityCommands : CommandGroup
    {
        [Command("dummy")]
        [CommandDescription("Replace the training room target dummy with the specified entity.")]
        [CommandUsage("entity dummy [pattern]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Dummy(string[] @params, NetClient client)
        {
            PrototypeId agentRef = CommandHelper.FindPrototype(HardcodedBlueprints.Agent, @params[0], client);
            if (agentRef == PrototypeId.Invalid) return string.Empty;
            var agentProto = GameDatabase.GetPrototype<AgentPrototype>(agentRef);

            PlayerConnection playerConnection = (PlayerConnection)client;
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


        [Command("marker")]
        [CommandDescription("Displays information about the specified marker.")]
        [CommandUsage("entity marker [MarkerId]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Marker(string[] @params, NetClient client)
        {
            if (int.TryParse(@params[0], out int markerId) == false)
                return $"Failed to parse MarkerId {@params[0]}";

            PlayerConnection playerConnection = (PlayerConnection)client;

            var reservation = playerConnection.AOI.Region.SpawnMarkerRegistry.GetReservationByPid(markerId);
            if (reservation == null) return "No marker found.";

            CommandHelper.SendMessage(client, $"Marker[{markerId}]: {GameDatabase.GetFormattedPrototypeName(reservation.MarkerRef)}");
            CommandHelper.SendMessageSplit(client, reservation.ToString(), false);
            return string.Empty;
        }


        [Command("info")]
        [CommandDescription("Displays information about the specified entity.")]
        [CommandUsage("entity info [EntityId]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Info(string[] @params, NetClient client)
        {
            if (ulong.TryParse(@params[0], out ulong entityId) == false)
                return $"Failed to parse EntityId {@params[0]}";

            Game game = ((PlayerConnection)client).Game;

            var entity = game.EntityManager.GetEntity<Entity>(entityId);
            if (entity == null) return "No entity found.";

            CommandHelper.SendMessage(client, $"Entity[{entityId}]: {GameDatabase.GetFormattedPrototypeName(entity.PrototypeDataRef)}");
            CommandHelper.SendMessageSplit(client, entity.Properties.ToString(), false);
            if (entity is WorldEntity worldEntity)
            {
                CommandHelper.SendMessageSplit(client, worldEntity.Bounds.ToString(), false);
                CommandHelper.SendMessageSplit(client, worldEntity.PowerCollectionToString(), false);
                CommandHelper.SendMessageSplit(client, worldEntity.ConditionCollectionToString(), false);
            }
            return string.Empty;
        }

        [Command("near")]
        [CommandDescription("Displays all entities in a radius (default is 100).")]
        [CommandUsage("entity near [radius]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        public string Near(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
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

            CommandHelper.SendMessage(client, $"Found for R={radius}:");
            CommandHelper.SendMessages(client, entities, false);
            return string.Empty;
        }

        [Command("isblocked")]
        [CommandUsage("entity isblocked [EntityId1] [EntityId2]")]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(2)]
        public string IsBlocked(string[] @params, NetClient client)
        {
            if (ulong.TryParse(@params[0], out ulong entityId1) == false)
                return $"Failed to parse EntityId1 {@params[0]}";

            if (ulong.TryParse(@params[1], out ulong entityId2) == false)
                return $"Failed to parse EntityId2 {@params[1]}";

            Game game = ((PlayerConnection)client).Game;
            var manager = game.EntityManager;

            var entity1 = manager.GetEntity<WorldEntity>(entityId1);
            if (entity1 == null) return $"No entity found for {entityId1}";

            var entity2 = manager.GetEntity<WorldEntity>(entityId2);
            if (entity2 == null) return $"No entity found for {entityId2}";

            Bounds bounds = entity1.Bounds;
            bool isBlocked = Region.IsBoundsBlockedByEntity(bounds, entity2, BlockingCheckFlags.CheckSpawns);
            return $"Entities\n [{entity1.PrototypeName}]\n [{entity2.PrototypeName}]\nIsBlocked: {isBlocked}";
        }

        [Command("tp")]
        [CommandDescription("Teleports to the first entity present in the region which prototype name contains the string given (ignore the case).")]
        [CommandUsage("entity tp [pattern]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Tp(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
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

        [Command("create")]
        [CommandDescription("Create entity near the avatar based on pattern (ignore the case) and count (default 1).")]
        [CommandUsage("entity create [pattern] [count]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Create(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection.Game;

            if (game == null)
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
                if (EntityHelper.GetSpawnPositionNearAvatar(avatar, region, agentProto.Bounds, 250, out Vector3 position) == false)
                    return "No space found to spawn the entity";
                var orientation = Orientation.FromDeltaVector(avatar.RegionLocation.Position - position);
                EntityHelper.CreateAgent(agentProto, avatar, position, orientation);
            }

            return $"Created!";
        }

        [Command("selector")]
        [CommandDescription("Create row entities near the avatar based on selector pattern (ignore the case).")]
        [CommandUsage("entity selector [pattern]")]
        [CommandUserLevel(AccountUserLevel.Admin)]
        [CommandInvokerType(CommandInvokerType.Client)]
        [CommandParamCount(1)]
        public string Selector(string[] @params, NetClient client)
        {
            PlayerConnection playerConnection = (PlayerConnection)client;
            Game game = playerConnection.Game;

            if (game == null)
                return "Game not found.";

            Avatar avatar = playerConnection.Player.CurrentAvatar;
            if (avatar == null || avatar.IsInWorld == false)
                return "Avatar not found.";

            Region region = avatar.Region;
            if (region == null) return "No region found.";

            PrototypeId selectorRef = CommandHelper.FindPrototype(HardcodedBlueprints.Selector, @params[0], client);
            if (selectorRef == PrototypeId.Invalid) return string.Empty;

            var selectorProto = GameDatabase.GetPrototype<EntitySelectorPrototype>(selectorRef);
            int rows = selectorProto.Entities.Length;
            if (rows == 0) return "Entities not found";

            if (selectorProto.EntitySelectorActions.Length == 0) return "Actions not found";
            var action = selectorProto.EntitySelectorActions[0];
            int cols = action.AIOverrides.Length;
            if (cols == 0) return "AIOverrides not found";

            float r = 100;
            var orientation = Orientation.Zero;
            var pos = avatar.RegionLocation.Position;
            float posX = pos.X - cols * r / 2;
            float posY = pos.Y - rows * r / 2;
            float startY = posY;

            for (int col = 0; col < cols; col++) 
            {
                var overrideRef = action.AIOverrides[col];
                var overrideProto = GameDatabase.GetPrototype<EntityActionAIOverridePrototype>(overrideRef);
                var powerRef = overrideProto.Power;
                posY = startY;
                for (int row = 0; row < rows; row++)
                {
                    var agentRef = selectorProto.Entities[row];
                    var agentProto = GameDatabase.GetPrototype<AgentPrototype>(agentRef);

                    var position = new Vector3(posX, posY, pos.Z);                    
                    var agent = EntityHelper.CreateAgent(agentProto, avatar, position, orientation);
                    if (agent != null)
                    {
                        PowerIndexProperties indexProps = new(0, agent.CharacterLevel, agent.CombatLevel);
                        agent.AssignPower(powerRef, indexProps);
                        PowerActivationSettings powerSettings = new(agent.Id, Vector3.Zero, agent.RegionLocation.Position);
                        powerSettings.Flags |= PowerActivationSettingsFlags.NotifyOwner;
                        agent.ActivatePower(powerRef, ref powerSettings);
                    }

                    posY += r;
                }
                posX += r;
            }

            return $"Created!";
        }
    }
}
