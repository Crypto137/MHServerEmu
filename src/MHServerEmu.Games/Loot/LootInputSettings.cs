using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    public class LootInputSettings : IPoolable, IDisposable
    {
        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }
        public WorldEntity SourceEntity { get; private set; }
        public Vector3? PositionOverride { get; private set; }

        public LootRollSettings LootRollSettings { get; private set; }

        // Settings for mission-specific drops
        public LootDropEventType EventType { get; set; } = LootDropEventType.None;
        public PrototypeId MissionProtoRef { get; set; } = PrototypeId.Invalid;

        public bool IsInPool { get; set; }

        public void Initialize(LootContext lootContext, Player player, WorldEntity sourceEntity, int level, Vector3? positionOverride = null)
        {
            LootContext = lootContext;
            Player = player;
            SourceEntity = sourceEntity;
            PositionOverride = positionOverride;

            Avatar avatar = player.CurrentAvatar;
            Region region = player.GetRegion();

            LootRollSettings = ObjectPoolManager.Instance.Get<LootRollSettings>();
            LootRollSettings.Player = player;
            LootRollSettings.UsableAvatar = avatar?.AvatarPrototype;
            LootRollSettings.UsablePercent = GameDatabase.LootGlobalsPrototype.LootUsableByRecipientPercent;
            LootRollSettings.Level = level;
            LootRollSettings.LevelForRequirementCheck = level;

            // Do not apply the timezone offset here because where our server is actually located
            // may not match the time zone defined in the globals prototype like the original game.
            LootRollSettings.UsableWeekday = LootUtilities.GetCurrentWeekday(false);    

            if (avatar != null && avatar.CurrentTeamUpAgent != null)
                LootRollSettings.UsableTeamUp = avatar.CurrentTeamUpAgent.AgentPrototype;

#if GAME_VERSION_1_53
            LootRollSettings.DifficultyTier = GetMissionDifficultyTier();
#endif

            if (sourceEntity != null)
            {
                if (LootRollSettings.DifficultyTier == PrototypeId.Invalid)
                    LootRollSettings.DifficultyTier = sourceEntity.Properties[PropertyEnum.DifficultyTier];

                if (sourceEntity.IsInWorld && avatar?.IsInWorld == true)
                    LootRollSettings.DropDistanceSq = Vector3.DistanceSquared2D(sourceEntity.RegionLocation.Position, avatar.RegionLocation.Position);

                LootRollSettings.SourceEntityKeywords = sourceEntity.KeywordsMask;
            }

            if (region != null)
            {
                if (LootRollSettings.DifficultyTier == PrototypeId.Invalid)
                    LootRollSettings.DifficultyTier = region.DifficultyTierRef;

                LootRollSettings.RegionScenarioRarity = region.Settings.ItemRarity;
            }

            LootRollSettings.AvatarConditionKeywords = avatar?.ConditionCollection?.ConditionKeywordsMask;
            LootRollSettings.RegionKeywords = avatar?.RegionLocation.GetKeywordsMask();
        }

        public void Initialize(LootContext lootContext, Player player, WorldEntity sourceEntity, Vector3? positionOverride = null)
        {
            Avatar avatar = player.CurrentAvatar;
            int level = 1;

            if (lootContext == LootContext.Drop && avatar != null)
                level = avatar.CharacterLevel;

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

            if (Verify.IsNotNull(LootRollSettings))
                pool.Return(LootRollSettings);

            pool.Return(this);
        }

#if GAME_VERSION_1_53
        private PrototypeId GetMissionDifficultyTier()
        {
            if (!Verify.IsNotNull(Player)) return PrototypeId.Invalid;
            
            if (LootRollSettings.MissionRef == PrototypeId.Invalid)
                return PrototypeId.Invalid;

            MissionPrototype missionProto = LootRollSettings.MissionRef.As<MissionPrototype>();
            if (!Verify.IsNotNull(missionProto)) return PrototypeId.Invalid;

            Mission mission;

            if (missionProto is OpenMissionPrototype)
                mission = Player.GetRegion()?.MissionManager.FindMissionByDataRef(missionProto.DataRef);
            else
                mission = Player.MissionManager.FindMissionByDataRef(missionProto.DataRef);

            return mission != null ? mission.DifficultyTierRef : PrototypeId.Invalid;
        }
#endif
    }
}
