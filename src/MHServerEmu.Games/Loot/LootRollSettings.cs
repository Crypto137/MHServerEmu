using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootRollSettings : IPoolable, IDisposable
    {
        public int Depth { get; set; }
        public LootDropChanceModifiers DropChanceModifiers { get; set; }
        public float NoDropModifier { get; set; } = 1f;         // LootRollModifyDropByDifficultyTierPrototype
        public Player Player { get; set; }                      // LootRollMissionStateRequiredPrototype
        public AvatarPrototype UsableAvatar { get; set; }       // LootRollSetAvatarPrototype
        public AgentPrototype UsableTeamUp { get; set; }        // Team-ups are the only agents other than avatars that have equipment
        public bool UseSecondaryAvatar { get; set; }            // LootNodePrototype::select()
        public bool ForceUsable { get; set; }                   // LootRollSetAvatarPrototype
        public float UsablePercent { get; set; }                // LootRollSetUsablePrototype

        public int Level { get; set; } = 1;                     // LootRollOffsetLevelPrototype
        public bool UseLevelVerbatim { get; set; }              // LootRollUseLevelVerbatimPrototype
        public int LevelForRequirementCheck { get; set; }       // LootRollRequireLevelPrototype

        public PrototypeId DifficultyTier { get; set; }
        public PrototypeId RegionScenarioRarity { get; set; }   // LootRollRequireRegionScenarioRarityPrototype
        public PrototypeId RegionAffixTable { get; set; }       // LootRollSetRegionAffixTablePrototype

        public int KillCount { get; set; }                          // LootRollRequireKillCountPrototype
        public Weekday UsableWeekday { get; set; } = Weekday.All;   // LootRollRequireWeekdayPrototype

        public HashSet<PrototypeId> Rarities { get; } = new();      // LootRollSetRarityPrototype

        public float DropDistanceSq { get; set; }                   // DistanceRestrictionPrototype::Allow()

        public KeywordsMask SourceEntityKeywords { get; set; } = KeywordsMask.Empty;
        public KeywordsMask AvatarConditionKeywords { get; set; } = KeywordsMask.Empty;
        public KeywordsMask RegionKeywords { get; set; } = KeywordsMask.Empty;

        // LootRollModifyAffixLimitsPrototype
        public Dictionary<AffixPosition, short> AffixLimitMinByPositionModifierDict { get; } = new();   // Modifies the minimum number of affixes for position
        public Dictionary<AffixPosition, short> AffixLimitMaxByPositionModifierDict { get; } = new();   // Modifies the maximum number of affixes for position
        public Dictionary<PrototypeId, short> AffixLimitByCategoryModifierDict { get; } = new();
        public PrototypeId MissionRef { get; set; }

        public bool IsInPool { get; set; }

        public LootRollSettings() { }   // Use pooling instead of calling this directly

        public void Set(LootRollSettings other)
        {
            Depth = other.Depth;
            DropChanceModifiers = other.DropChanceModifiers;
            NoDropModifier = other.NoDropModifier;

            Player = other.Player;
            UsableAvatar = other.UsableAvatar;
            UsableTeamUp = other.UsableTeamUp;
            UseSecondaryAvatar = other.UseSecondaryAvatar;
            ForceUsable = other.ForceUsable;
            UsablePercent = other.UsablePercent;

            Level = other.Level;
            UseLevelVerbatim = other.UseLevelVerbatim;
            LevelForRequirementCheck = other.LevelForRequirementCheck;

            DifficultyTier = other.DifficultyTier;
            RegionScenarioRarity = other.RegionScenarioRarity;
            RegionAffixTable = other.RegionAffixTable;

            KillCount = other.KillCount;
            UsableWeekday = other.UsableWeekday;

            Rarities.Clear();
            if (other.Rarities.Count > 0)
            {
                foreach (PrototypeId rarityProtoRef in other.Rarities)
                    Rarities.Add(rarityProtoRef);
            }

            DropDistanceSq = other.DropDistanceSq;

            SourceEntityKeywords = other.SourceEntityKeywords;
            AvatarConditionKeywords = other.AvatarConditionKeywords;
            RegionKeywords = other.RegionKeywords;

            AffixLimitMinByPositionModifierDict.Clear();
            if (other.AffixLimitMinByPositionModifierDict.Count > 0)
            {
                foreach (var kvp in other.AffixLimitMinByPositionModifierDict)
                    AffixLimitMinByPositionModifierDict.Add(kvp.Key, kvp.Value);
            }

            AffixLimitMaxByPositionModifierDict.Clear();
            if (other.AffixLimitMaxByPositionModifierDict.Count > 0)
            {
                foreach (var kvp in other.AffixLimitMaxByPositionModifierDict)
                    AffixLimitMaxByPositionModifierDict.Add(kvp.Key, kvp.Value);
            }

            AffixLimitByCategoryModifierDict.Clear();
            if (other.AffixLimitByCategoryModifierDict.Count > 0)
            {
                foreach (var kvp in other.AffixLimitByCategoryModifierDict)
                    AffixLimitByCategoryModifierDict.Add(kvp.Key, kvp.Value);
            }
        }

        public void ResetForPool()
        {
            Depth = default;
            DropChanceModifiers = default;
            NoDropModifier = 1f;

            Player = default;
            UsableAvatar = default;
            UsableTeamUp = default;
            UseSecondaryAvatar = default;
            ForceUsable = default;
            UsablePercent = default;

            Level = 1;
            UseLevelVerbatim = default;
            LevelForRequirementCheck = default;

            DifficultyTier = default;
            RegionScenarioRarity = default;
            RegionAffixTable = default;

            KillCount = default;
            UsableWeekday = Weekday.All;

            Rarities.Clear();

            DropDistanceSq = default;

            SourceEntityKeywords = KeywordsMask.Empty;
            AvatarConditionKeywords = KeywordsMask.Empty;
            RegionKeywords = KeywordsMask.Empty;

            AffixLimitMinByPositionModifierDict.Clear();
            AffixLimitMaxByPositionModifierDict.Clear();
            AffixLimitByCategoryModifierDict.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }

        /// <summary>
        /// Returns <see langword="true"/> if these <see cref="LootRollSettings"/> contain any restriction <see cref="LootDropChanceModifiers"/>.
        /// </summary>
        /// <remarks>
        /// Restriction flags are DifficultyModeRestricted, RegionRestricted, KillCountRestricted, WeekdayRestricted,
        /// ConditionRestricted, DifficultyTierRestricted, LevelRestricted, DropperRestricted, and MissionRestricted.
        /// </remarks>
        public bool IsRestrictedByLootDropChanceModifier()
        {
            return DropChanceModifiers.HasFlag(LootDropChanceModifiers.DifficultyModeRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.RegionRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.KillCountRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.WeekdayRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.ConditionRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.DifficultyTierRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.LevelRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.DropperRestricted) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.MissionRestricted);
        }

        /// <summary>
        /// Returns <see langword="true"/> if these <see cref="LootRollSettings"/> contain any cooldown <see cref="LootDropChanceModifiers"/>.
        /// </summary>
        /// <remarks>
        /// Cooldown flags are CooldownOncePerXHours, CooldownOncePerRollover, and CooldownOncePerXHours.
        /// </remarks>
        public bool HasCooldownLootDropChanceModifier()
        {
            return DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerXHours) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownOncePerRollover) ||
                   DropChanceModifiers.HasFlag(LootDropChanceModifiers.CooldownByChannel);
        }
    }
}
