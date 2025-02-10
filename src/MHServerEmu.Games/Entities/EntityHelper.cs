using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    /// <summary>
    /// A helper class for managing hardcoded entities. TODO: Gradually get rid of stuff in here.
    /// </summary>
    public static class EntityHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public static readonly bool DebugOrb = false;

        public enum TestOrb : ulong
        {
            Red = 925659119519994384, // HealOrbItem = 925659119519994384, 
            BigRed = 18107188791044543532, // LimboBuffOrbItem = 18107188791044543532,
            Greeen = 16852724980331648695, // ExperienceOrbSmallItem = 16852724980331648695,
            BigGreen = 3442167663578518146, // LegendaryOrb = 3442167663578518146,
            Blue = 9607833165236212779, // EnduranceOrbItem = 9607833165236212779
            Orange = 8905675869072986929, // TestOnlyXPOrb = 8905675869072986929,
            XRay = 5358798066155328438, // Radioactive31Orb = 5358798066155328438,
            Pink = 14631580738344719410, // ManhattanOrbItem = 14631580738344719410,
            Hyde = 1644714682932532551, // Art252HydeFormulaOrb = 1644714682932532551,
            Violet = 18337403507337860830, // MagnetoMetalOrb = 18337403507337860830,
        }

        public static Agent CrateOrb(TestOrb orbProto, Vector3 position, Region region)
        {
            if (DebugOrb == false) return null;

            var game = region.Game;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = (PrototypeId)orbProto;
            settings.Position = position;
            settings.Orientation = new(3.14f, 0.0f, 0.0f);
            settings.RegionId = region.Id;
            settings.Lifespan = TimeSpan.FromSeconds(3);

            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            properties[PropertyEnum.AIStartsEnabled] = false;
            properties[PropertyEnum.NoEntityCollide] = true;
            settings.Properties = properties;

            Agent orb = (Agent)game.EntityManager.CreateEntity(settings);
            return orb;
        }

        public static void OnDeathSummonFromPowerPrototype(WorldEntity entity, SummonPowerPrototype summonPowerProto)
        {
            AssetId creatorAsset = entity.GetEntityWorldAsset();
            if (summonPowerProto.SummonEntityContexts.IsNullOrEmpty()) return;
            PrototypeId summonerRef = summonPowerProto.SummonEntityContexts[0].SummonEntity;
            var summonerProto = entity.WorldEntityPrototype;

            var summonProto = GameDatabase.GetPrototype<AgentPrototype>(summonerRef);
            if (summonProto == null) return; // Only Agent can be spawn, skip hotspot

            Logger.Debug($"OnDeathSummonFromPowerPrototype(): {summonPowerProto}");

            using (EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>())
            using (PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>())
            {
                settings.EntityRef = summonerRef;
                settings.Position = entity.RegionLocation.Position;
                settings.Orientation = entity.RegionLocation.Orientation;
                settings.RegionId = entity.Region.Id;

                properties[PropertyEnum.CreatorEntityAssetRefBase] = creatorAsset;
                properties[PropertyEnum.CreatorEntityAssetRefCurrent] = creatorAsset;
                properties[PropertyEnum.CreatorPowerPrototype] = summonPowerProto.DataRef;
                properties[PropertyEnum.SummonedByPower] = true;
                properties[PropertyEnum.Rank] = summonerProto.Rank;
                settings.Properties = properties;

                var group = entity.SpawnGroup;

                if (group != null && summonPowerProto.SummonAsPopulation)
                {
                    var spec = entity.Region.PopulationManager.CreateSpawnSpec(group);
                    spec.EntityRef = summonerRef;
                    spec.Transform = Transform3.BuildTransform(settings.Position - group.Transform.Translation, settings.Orientation);
                    spec.Properties.FlattenCopyFrom(properties, false);
                    spec.Spawn();
                }
                else
                {
                    Agent summoner = (Agent)entity.Game.EntityManager.CreateEntity(settings);
                }
            }
        }

        public static void CreateMetalOrbFromPowerPrototype(WorldEntity user, WorldEntity target, Vector3 targetPosition, SummonPowerPrototype summonPowerProto)
        {
            if (user is not Avatar avatar)
                return;

            if (avatar.IsInWorld == false)
                return;

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null)
                return;

            if (summonPowerProto.SummonEntityContexts.IsNullOrEmpty())
                return;

            PrototypeId summonEntityRef = summonPowerProto.SummonEntityContexts[0].SummonEntity;

            Region region = avatar.Region;
            Bounds bounds = target?.Bounds;
            if (bounds == null)
            {
                bounds = new();
                bounds.InitializeSphere(35f, BoundsCollisionType.None);
                bounds.Center = targetPosition;
            }            

            if (region.ChooseRandomPositionNearPoint(bounds, PathFlags.Walk, PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns, 0f, 100f, out Vector3 position) == false)
            {
                Logger.Warn($"CreateMetalOrbFromPowerPrototype(): Failed to find a position to summon entity {summonEntityRef.GetName()} near [{targetPosition}] in region [{region}]");
                return;
            }

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            using PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            settings.EntityRef = summonEntityRef;
            settings.Position = position;
            settings.RegionId = avatar.Region.Id;

            if (target != null)
            {
                settings.SourceEntityId = target.Id;
                settings.SourcePosition = target.RegionLocation.Position;
            }

            properties[PropertyEnum.CreatorEntityAssetRefBase] = avatar.GetOriginalWorldAsset();
            properties[PropertyEnum.CreatorEntityAssetRefCurrent] = avatar.GetEntityWorldAsset();
            properties[PropertyEnum.CreatorPowerPrototype] = summonPowerProto.DataRef;
            properties[PropertyEnum.SummonedByPower] = true;
            properties[PropertyEnum.RestrictedToPlayerGuid] = player.DatabaseUniqueId;
            settings.Properties = properties;

            avatar.Game.EntityManager.CreateEntity(settings);
        }

        public static void SummonEntityFromPowerPrototype(Avatar avatar, SummonPowerPrototype summonPowerProto, Item item = null)
        {
            AssetId creatorAsset = avatar.GetEntityWorldAsset();
            PrototypeId allianceRef = (PrototypeId)15452561577132953366;    // Entity/Alliances/NPCNeutral.prototype, should be set by EvalOnSummon

            if (summonPowerProto.SummonEntityContexts.IsNullOrEmpty()) return;
            PrototypeId summonerRef = summonPowerProto.SummonEntityContexts[0].SummonEntity;
            var summonerProto = GameDatabase.GetPrototype<WorldEntityPrototype>(summonerRef);
            if (summonerProto == null) return;
            if (summonerProto is not AgentPrototype && summonerProto is not TransitionPrototype) return;

            WorldEntity summoner;
            using (EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>())
            using (PropertyCollection properties = ObjectPoolManager.Instance.Get<PropertyCollection>())
            {
                settings.EntityRef = summonerRef;

                // DangerRoomScenario
                if (summonerProto is TransitionPrototype && item != null)
                {
                    item.SetScenarioProperties(properties);

                    var player = avatar.GetOwnerOfType<Player>();
                    properties[PropertyEnum.RestrictedToPlayerGuidParty] = player.DatabaseUniqueId;
                }

                properties[PropertyEnum.NoMissileCollide] = true; // EvalOnCreate
                properties[PropertyEnum.CreatorEntityAssetRefBase] = creatorAsset;
                properties[PropertyEnum.CreatorEntityAssetRefCurrent] = creatorAsset;
                properties[PropertyEnum.CreatorPowerPrototype] = summonPowerProto.DataRef;
                properties[PropertyEnum.SummonedByPower] = true;
                properties[PropertyEnum.AllianceOverride] = allianceRef;
                properties[PropertyEnum.Rank] = summonerProto.Rank;
                settings.Properties = properties;

                summoner = (WorldEntity)avatar.Game.EntityManager.CreateEntity(settings);
            }

            using (EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>())
            {
                settings.OptionFlags = EntitySettingsOptionFlags.IsNewOnServer;
                summoner.EnterWorld(avatar.Region, summoner.GetPositionNearAvatar(avatar), avatar.RegionLocation.Orientation, settings);
            }

            if (summoner is Agent agent && summonPowerProto.ActionsTriggeredOnPowerEvent.HasValue())
                agent.AIController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = avatar.Id;
            summoner.Properties[PropertyEnum.PowerUserOverrideID] = avatar.Id;

            Inventory summonedInventory = avatar.GetInventory(InventoryConvenienceLabel.Summoned);
            summoner.ChangeInventoryLocation(summonedInventory);
        }

        public static void DestroySummonerFromPowerPrototype(Avatar avatar, SummonPowerPrototype summonPowerProto)
        {
            var summonerProto = summonPowerProto.GetSummonEntity(0, avatar.GetOriginalWorldAsset());
            Inventory summonedInventory = avatar.GetInventory(InventoryConvenienceLabel.Summoned);
            Agent summoner = summonedInventory.GetMatchingEntity(summonerProto.DataRef) as Agent;
            summoner?.Destroy();
        }

        private static PrototypeId GetVisibleParentRef(PrototypeId invisibleId)
        {
            WorldEntityPrototype invisibleProto = GameDatabase.GetPrototype<WorldEntityPrototype>(invisibleId);
            if (invisibleProto.VisibleByDefault == false) return GetVisibleParentRef(invisibleProto.ParentDataRef);
            return invisibleId;
        }
    }
}
