using MHServerEmu.Games.Missions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootRollModifierPrototype : Prototype
    {
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }
    }

    public class LootRollMarkSpecialPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollUnmarkSpecialPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMarkRarePrototype : LootRollModifierPrototype
    {
    }

    public class LootRollUnmarkRarePrototype : LootRollModifierPrototype
    {
    }

    public class LootRollOffsetLevelPrototype : LootRollModifierPrototype
    {
        public int LevelOffset { get; protected set; }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level { get; protected set; }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position { get; protected set; }
        public short ModifyMinBy { get; protected set; }
        public short ModifyMaxBy { get; protected set; }
        public PrototypeId Category { get; protected set; }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable { get; protected set; }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim { get; protected set; }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public CurveId ModifierCurve { get; protected set; }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired { get; protected set; }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public Weekday[] Choices { get; protected set; }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public PrototypeId RegionAffixTable { get; protected set; }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Missions { get; protected set; }
        public MissionState RequiredState { get; protected set; }
    }
}
