using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootInputSettings : IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }
        public WorldEntity SourceEntity { get; private set; }
        public Vector3? PositionOverride { get; private set; }

        public LootRollSettings LootRollSettings { get; private set; }

        public void Initialize(LootContext lootContext, Player player, WorldEntity sourceEntity, int level, Vector3? positionOverride = null)
        {
            LootContext = lootContext;
            Player = player;
            SourceEntity = sourceEntity;
            PositionOverride = positionOverride;

            Avatar avatar = player.CurrentAvatar;
            Region region = player.GetRegion();

            LootRollSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            LootRollSettings.UsableAvatar = avatar?.AvatarPrototype;
            LootRollSettings.UsablePercent = GameDatabase.LootGlobalsPrototype.LootUsableByRecipientPercent;
            LootRollSettings.Level = level;
            LootRollSettings.LevelForRequirementCheck = level;

            if (region != null)
            {
                LootRollSettings.DifficultyTier = region.DifficultyTierRef;
                LootRollSettings.RegionScenarioRarity = region.Settings.ItemRarity;
            }
        }

        public void Initialize(LootContext lootContext, Player player, WorldEntity sourceEntity, Vector3? positionOverride = null)
        {
            Avatar avatar = player.CurrentAvatar;
            int level;

            if (avatar != null)
            {
                level = avatar.CharacterLevel;
            }
            else
            {
                Logger.Warn("Initialize(): Player has no current avatar to get level from, defaulting to 1");
                level = 1;
            }

            Initialize(lootContext, player, sourceEntity, level, positionOverride);
        }

        public void ResetForPool()
        {
            LootContext = default;
            Player = default;
            SourceEntity = default;
            PositionOverride = default;

            LootRollSettings = default;
        }

        public void Dispose()
        {
            ObjectPoolManager pool = ObjectPoolManager.Instance;

            if (LootRollSettings != null)
                pool.Return(LootRollSettings);
            else
                Logger.Warn("Dispose(): LootRollSettings == null");

            pool.Return(this);
        }
    }
}
