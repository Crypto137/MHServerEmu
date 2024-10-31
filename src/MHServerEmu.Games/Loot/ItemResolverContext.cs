using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// Contains context data for rolling loot tables.
    /// </summary>
    public class ItemResolverContext
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private CooldownData _cooldownData = new();

        public LootContext LootContext { get; private set; }
        public Player Player { get; private set; }

        public Region Region { get => Player?.GetRegion(); }

        public void Set(LootContext lootContext, Player player, WorldEntity sourceEntity = null)
        {
            LootContext = lootContext;
            Player = player;

            InitializeCooldownData(sourceEntity);
        }

        public float GetDropChance(LootRollSettings settings, float noDropPercent)
        {
            // Do not drop if there are any hard restrictions (this should have already been handled when selecting the loot table node)
            if (settings.IsRestrictedByLootDropChanceModifier())
                return Logger.WarnReturn(0f, $"GetDropChance(): Restricted by loot drop chance modifiers [{settings.DropChanceModifiers}]");

            if (settings.HasCooldownLootDropChanceModifier() &&
                settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.IgnoreCooldown) == false)
            {
                // If this roll requires a cooldown, make sure we have a valid cooldown origin
                if (_cooldownData.OriginProtoRef == PrototypeId.Invalid)
                    return Logger.WarnReturn(0f, "GetDropChance(): Failed to determine cooldown origin");

                // Cooldowns can be per-account or per-avatar
                bool cooldownActive = settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PerAccount)
                    ? _cooldownData.ActiveOnPlayer
                    : _cooldownData.ActiveOnAvatar;

                // Do not drop anything for this roll if the cooldown is active
                if (cooldownActive)
                    return 0f;

                // Set the cooldown
                SetDropChanceCooldown(settings);
            }

            // Start with a base drop chance based on the specified NoDrop percent
            float dropChance = 1f - noDropPercent;

            // Apply live tuning multiplier
            dropChance *= LiveTuningManager.GetLiveGlobalTuningVar(GlobalTuningVar.eGTV_LootDropRate);

            // Apply difficulty multiplier
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.DifficultyTierNoDropModified))
                dropChance *= settings.NoDropModifier;

            // Add more multipliers here as needed

            return dropChance;
        }

        public bool IsOnCooldown(PrototypeId dropProtoRef, int count)
        {
            // Check if cooldowns are applicable in this loot context (e.g. crafting should not have any cooldowns)
            if (LootContext != LootContext.Drop && LootContext != LootContext.MissionReward)
                return false;

            // Check if this drop has a cooldown channel
            LootCooldownChannelPrototype cooldownChannelProto = GameDataTables.Instance.LootCooldownTable.GetCooldownChannelForLoot(dropProtoRef);
            if (cooldownChannelProto == null)
                return false;

            bool isOnCooldown = cooldownChannelProto.IsOnCooldown(Player.Game, Player.Properties);

            // Set cooldown if this drop wasn't on cooldown
            if (isOnCooldown == false)
                cooldownChannelProto.SetCooldown(Player, count);

            //Logger.Debug($"IsOnCooldown(): {dropProtoRef.GetName()} x{count} = {isOnCooldown}");
            return isOnCooldown;
        }

        private bool InitializeCooldownData(WorldEntity sourceEntity)
        {
            _cooldownData.Clear();

            if (FindCooldownOrigin(sourceEntity, out LootCooldownType cooldownType) == false)
                return false;

            Player player = Player;
            if (player == null) return Logger.WarnReturn(false, "InitializeCooldownData(): player == null");

            if (cooldownType == LootCooldownType.ByChannel)
            {
                // For channel-based cooldowns use the prototype to initialize cooldown data
                LootCooldownChannelPrototype lootCooldownChannelProto = _cooldownData.OriginProtoRef.As<LootCooldownChannelPrototype>();
                if (lootCooldownChannelProto == null) return Logger.WarnReturn(false, "InitializeCooldownData(): lootCooldownChannelProto == null");

                lootCooldownChannelProto.GetCooldownSettings(player,
                    out _cooldownData.PropertyEnum, out _cooldownData.ActiveOnPlayer, out _cooldownData.ActiveOnAvatar, out _cooldownData.Time);
            }
            else if (_cooldownData.PropertyEnum != PropertyEnum.Invalid)
            {
                // This should be either LootCooldownTimeStartEntity or LootCooldownTimeStartRegion
                PropertyId cooldownProperty = new(_cooldownData.PropertyEnum, _cooldownData.OriginProtoRef, _cooldownData.DifficultyProtoRef);

                if (cooldownType == LootCooldownType.TimeHours)
                {
                    _cooldownData.Time = player.Game.CurrentTime;   // Use game time for hour-based cooldowns

                    TimeSpan timeHours = TimeSpan.FromHours((int)sourceEntity.Properties[PropertyEnum.LootCooldownTimeHours]);

                    _cooldownData.ActiveOnPlayer = timeHours > _cooldownData.Time - player.Properties[cooldownProperty];

                    Avatar avatar = player.CurrentAvatar;
                    if (avatar != null)
                    {
                        _cooldownData.ActiveOnAvatar = timeHours > _cooldownData.Time - avatar.Properties[cooldownProperty];
                    }
                    else
                    {
                        Logger.Warn("InitializeCooldownData(): avatar == null");
                        _cooldownData.ActiveOnAvatar = true;
                    }
                }
                else if (cooldownType == LootCooldownType.RolloverWallTime)
                {
                    _cooldownData.Time = Clock.UnixTime;            // Use Unix time for rollover cooldowns

                    if (LootUtilities.GetLastLootCooldownRolloverWallTime(sourceEntity.Properties, _cooldownData.Time, out TimeSpan lastRolloverTime) == false)
                        return Logger.WarnReturn(false, "InitializeCooldownData(): Failed to get last loot cooldown rollover wall time");

                    _cooldownData.ActiveOnPlayer = player.Properties[cooldownProperty] > lastRolloverTime;

                    Avatar avatar = player.CurrentAvatar;
                    if (avatar != null)
                    {
                        _cooldownData.ActiveOnAvatar = avatar.Properties[cooldownProperty] > lastRolloverTime;
                    }
                    else
                    {
                        Logger.Warn("InitializeCooldownData(): avatar == null");
                        _cooldownData.ActiveOnAvatar = true;
                    }
                }
            }

            return true;
        }

        private bool FindCooldownOrigin(WorldEntity sourceEntity, out LootCooldownType cooldownType)
        {
            // Cooldown start time properties for reference:
            // LootCooldownTimeStartChannel, LootCooldownTimeStartEntity, LootCooldownTimeStartRegion, LootCooldownTimeStartSpecial

            // TODO: Region cooldowns? Is this even used?
            // We may not need special cooldowns since no special table is set in the loot globals prototype in 1.52 (TODO: check other versions)

            cooldownType = LootCooldownType.Invalid;

            if (sourceEntity == null)
                return false;

            // Determine cooldown type (if any)
            if (sourceEntity.Properties.HasProperty(PropertyEnum.LootCooldownByChannel))
                cooldownType = LootCooldownType.ByChannel;
            else if (sourceEntity.Properties.HasProperty(PropertyEnum.LootCooldownTimeHours))
                cooldownType = LootCooldownType.TimeHours;
            else if (sourceEntity.Properties.HasProperty(PropertyEnum.LootCooldownRolloverWallTime))
                cooldownType = LootCooldownType.RolloverWallTime;

            // Initialize cooldown data
            if (cooldownType == LootCooldownType.ByChannel)
            {
                _cooldownData.OriginProtoRef = sourceEntity.Properties[PropertyEnum.LootCooldownByChannel];
                cooldownType = LootCooldownType.ByChannel;
                return true;
            }
            else if (cooldownType == LootCooldownType.TimeHours || cooldownType == LootCooldownType.RolloverWallTime)
            {
                _cooldownData.OriginProtoRef = sourceEntity.PrototypeDataRef;
                _cooldownData.DifficultyProtoRef = sourceEntity.Region.DifficultyTierRef;
                _cooldownData.PropertyEnum = PropertyEnum.LootCooldownTimeStartEntity;

                return true;
            }

            return false;
        }

        private bool SetDropChanceCooldown(LootRollSettings settings)
        {
            //Logger.Debug($"SetDropChanceCooldown(): {_cooldownData.OriginProtoRef.GetName()} for {Player}");

            // No need to set cooldown if we are just doing a preview roll
            if (settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PreviewOnly))
                return true;

            PropertyCollection properties = settings.DropChanceModifiers.HasFlag(LootDropChanceModifiers.PerAccount)
                ? Player?.Properties
                : Player?.CurrentAvatar?.Properties;

            if (properties == null) return Logger.WarnReturn(false, "SetDropChanceCooldown(): properties == null");

            PropertyId cooldownProperty = _cooldownData.GetCooldownProperty();
            if (cooldownProperty.Enum == PropertyEnum.Invalid) return Logger.WarnReturn(false, "SetDropChanceCooldown(): cooldownProperty.Enum == PropertyEnum.Invalid");

            properties[cooldownProperty] = _cooldownData.Time;

            // NOTE: LootCooldownHierarchyPrototype doesn't seem to have any valid data in version 1.52

            return true;
        }

        private struct CooldownData
        {
            public PropertyEnum PropertyEnum;
            public PrototypeId OriginProtoRef;
            public PrototypeId DifficultyProtoRef;
            public bool ActiveOnPlayer;
            public bool ActiveOnAvatar;
            public TimeSpan Time;

            public void Clear()
            {
                PropertyEnum = PropertyEnum.Invalid;
                OriginProtoRef = default;
                DifficultyProtoRef = default;
                ActiveOnPlayer = default;
                ActiveOnAvatar = default;
                Time = default;
            }

            public PropertyId GetCooldownProperty()
            {
                // Channel cooldowns do not have a difficulty param
                if (PropertyEnum == PropertyEnum.LootCooldownTimeStartChannel)
                    return new(PropertyEnum, OriginProtoRef);

                return new(PropertyEnum, OriginProtoRef, DifficultyProtoRef);
            }
        }
    }
}
