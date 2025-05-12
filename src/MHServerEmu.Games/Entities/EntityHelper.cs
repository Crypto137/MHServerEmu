using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
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

        public static Agent CreateAgent(AgentPrototype agentProto, Avatar avatar, Vector3 spawnPosition, Orientation orientation)
        {
            var region = avatar.Region;

            using EntitySettings entitySettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            entitySettings.EntityRef = agentProto.DataRef;
            entitySettings.Position = spawnPosition;
            entitySettings.Orientation = orientation;
            entitySettings.RegionId = region.Id;
            entitySettings.IsPopulation = true;

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settingsProperties[PropertyEnum.DifficultyTier] = region.DifficultyTierRef;
            settingsProperties[PropertyEnum.Rank] = agentProto.Rank;
            settingsProperties[PropertyEnum.CharacterLevel] = avatar.CharacterLevel;
            settingsProperties[PropertyEnum.CombatLevel] = avatar.CharacterLevel;
            entitySettings.Properties = settingsProperties;

            var agent = avatar.Game.EntityManager.CreateEntity(entitySettings) as Agent;

            if (agentProto.ModifiersGuaranteed.HasValue())
                foreach (var boost in agentProto.ModifiersGuaranteed)
                    agent.Properties[PropertyEnum.EnemyBoost, boost] = true;

            return agent;
        }

        public static bool GetSpawnPositionNearAvatar(Avatar avatar, Region region, BoundsPrototype entityBoundsPrototype, float maxDistance, out Vector3 spawnPositionResult)
        {
            Bounds entityBounds = new();
            entityBounds.InitializeFromPrototype(entityBoundsPrototype);
            entityBounds.Center = avatar.RegionLocation.Position + avatar.Forward * 120;
            return region.ChoosePositionAtOrNearPoint(entityBounds, avatar.Locomotor.PathFlags, PositionCheckFlags.CanBeBlockedEntity, BlockingCheckFlags.None, maxDistance, out spawnPositionResult, maxPositionTests: 32);
        }
    }
}
