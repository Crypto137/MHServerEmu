using MHServerEmu.Core.Memory;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootRollSettings : IPoolable, IDisposable
    {
        public int Depth { get; set; }
        public LootDropChanceModifiers DropChanceModifiers { get; set; }
        public float NoDropModifier { get; set; } = 1f;         // LootRollModifyDropByDifficultyTierPrototype

        public AvatarPrototype UsableAvatar { get; set; }       // LootRollSetAvatarPrototype
        public AgentPrototype UsableTeamUp { get; set; }        // Team-ups are the only agents other than avatars that have equipment
        public bool UseSecondaryAvatar { get; set; }            // LootNodePrototype::select()
        public bool ForceUsable { get; set; }                   // LootRollSetAvatarPrototype
        public float UsablePercent { get; set; }                // LootRollSetUsablePrototype

        public int Level { get; set; } = 1;                     // LootRollOffsetLevelPrototype
        public bool UseLevelVerbatim { get; set; } = false;     // LootRollUseLevelVerbatimPrototype
        public int LevelForRequirementCheck { get; set; } = 0;  // LootRollRequireLevelPrototype

        public PrototypeId DifficultyTier { get; set; }
        public PrototypeId RegionScenarioRarity { get; set; }   // LootRollRequireRegionScenarioRarityPrototype
        public PrototypeId RegionAffixTable { get; set; }       // LootRollSetRegionAffixTablePrototype

        public int KillCount { get; set; } = 0;                 // LootRollRequireKillCountPrototype
        public Weekday UsableWeekday { get; set; } = Weekday.All;   // LootRollRequireWeekdayPrototype

        public HashSet<PrototypeId> Rarities { get; } = new();  // LootRollSetRarityPrototype

        public float DropDistanceThresholdSq { get; set; }      // DistanceRestrictionPrototype::Allow()

        // LootRollModifyAffixLimitsPrototype
        public Dictionary<AffixPosition, short> AffixLimitMinByPositionModifierDict { get; } = new();   // Modifies the minimum number of affixes for position
        public Dictionary<AffixPosition, short> AffixLimitMaxByPositionModifierDict { get; } = new();   // Modifies the maximum number of affixes for position
        public Dictionary<PrototypeId, short> AffixLimitByCategoryModifierDict { get; } = new();

        public LootRollSettings() { }   // Use pooling instead of calling this directly

        public void Set(LootRollSettings other)
        {
            Depth = other.Depth;
            DropChanceModifiers = other.DropChanceModifiers;
            NoDropModifier = other.NoDropModifier;

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

            DropDistanceThresholdSq = other.DropDistanceThresholdSq;

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
            Depth = 0;
            DropChanceModifiers = LootDropChanceModifiers.None;
            NoDropModifier = 1f;

            UsableAvatar = null;
            UsableTeamUp = null;
            UseSecondaryAvatar = false;
            ForceUsable = false;
            UsablePercent = 0f;

            Level = 1;
            UseLevelVerbatim = false;
            LevelForRequirementCheck = 0;

            DifficultyTier = PrototypeId.Invalid;
            RegionScenarioRarity = PrototypeId.Invalid;
            RegionAffixTable = PrototypeId.Invalid;

            KillCount = 0;
            UsableWeekday = Weekday.All;

            Rarities.Clear();

            DropDistanceThresholdSq = 0f;

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
    }
}
