using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
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

            LootRollSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            LootRollSettings.UsableAvatar = player.CurrentAvatar.AvatarPrototype;
            LootRollSettings.UsablePercent = GameDatabase.LootGlobalsPrototype.LootUsableByRecipientPercent;
            LootRollSettings.Level = player.CurrentAvatar.CharacterLevel;
            LootRollSettings.LevelForRequirementCheck = player.CurrentAvatar.CharacterLevel;
            LootRollSettings.DifficultyTier = player.GetRegion().DifficultyTierRef;
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
