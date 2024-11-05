using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Loot
{
    public class LootInputSettings : IPoolable, IDisposable
    {
        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }
        public WorldEntity SourceEntity { get; private set; }
        public Vector3? PositionOverride { get; private set; }

        public LootRollSettings LootRollSettings { get; private set; }

        public void Initialize(LootContext lootContext, Player player, WorldEntity sourceEntity, Vector3? positionOverride = null)
        {
            LootContext = lootContext;
            Player = player;
            SourceEntity = sourceEntity;
            PositionOverride = positionOverride;

            Avatar avatar = player.CurrentAvatar;

            LootRollSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            LootRollSettings.UsableAvatar = avatar.AvatarPrototype;
            LootRollSettings.UsablePercent = GameDatabase.LootGlobalsPrototype.LootUsableByRecipientPercent;
            LootRollSettings.Level = avatar.CharacterLevel;
            LootRollSettings.LevelForRequirementCheck = avatar.CharacterLevel;
            LootRollSettings.DifficultyTier = player.GetRegion().DifficultyTierRef;
            LootRollSettings.RegionScenarioRarity = player.GetRegion().Settings.ItemRarity;
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
            pool.Return(LootRollSettings);
            pool.Return(this);
        }
    }
}
