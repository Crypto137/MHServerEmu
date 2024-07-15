using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootRollSettings
    {
        public int Depth { get; set; }
        public LootDropChanceModifiers DropChanceModifiers { get; set; }
        public float NoDropModifier { get; set; } = 1f;         // LootRollModifyDropByDifficultyTierPrototype

        public AvatarPrototype UsableAvatar { get; set; }       // LootRollSetAvatarPrototype
        public AgentPrototype UsableTeamUp { get; set; }        // Team-ups are the only agents other than avatars that have equipment
        public bool UseSecondaryAvatar { get; set; }            // LootNodePrototype::select()
        public bool HasUsableOverride { get; set; }             // LootRollSetAvatarPrototype
        public float UsableOverrideValue { get; set; }          // LootRollSetUsablePrototype

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
        public Dictionary<AffixPosition, short> AffixLimitByMinPositionModifierDict { get; } = new();
        public Dictionary<AffixPosition, short> AffixLimitByMaxPositionModifierDict { get; } = new();
        public Dictionary<PrototypeId, short> AffixLimitByCategoryModifierDict { get; } = new();

        public LootRollSettings() { }

        public LootRollSettings(LootRollSettings other)
        {
            Depth = other.Depth;
            DropChanceModifiers = other.DropChanceModifiers;
            NoDropModifier = other.NoDropModifier;

            UsableAvatar = other.UsableAvatar;
            UsableTeamUp = other.UsableTeamUp;
            UseSecondaryAvatar = other.UseSecondaryAvatar;
            HasUsableOverride = other.HasUsableOverride;
            UsableOverrideValue = other.UsableOverrideValue;

            Level = other.Level;
            UseLevelVerbatim = other.UseLevelVerbatim;
            LevelForRequirementCheck = other.LevelForRequirementCheck;

            DifficultyTier = other.DifficultyTier;
            RegionScenarioRarity = other.RegionScenarioRarity;
            RegionAffixTable = other.RegionAffixTable;

            KillCount = other.KillCount;
            UsableWeekday = other.UsableWeekday;

            Rarities = new(other.Rarities);

            DropDistanceThresholdSq = other.DropDistanceThresholdSq;

            AffixLimitByMinPositionModifierDict = new(other.AffixLimitByMinPositionModifierDict);
            AffixLimitByMaxPositionModifierDict = new(other.AffixLimitByMaxPositionModifierDict);
            AffixLimitByCategoryModifierDict = new(other.AffixLimitByCategoryModifierDict);
        }
    }
}
