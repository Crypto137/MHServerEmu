

using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class LootTablePrototype : LootDropPrototype
    {
        public ulong Choices;
        public ulong MissionLogRewardsText;
        public float NoDropPercent;
        public int PickMethod;
        public bool LiveTuningDefaultEnabled;
        public LootTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootTablePrototype), proto); }

    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin;
        public short NumMax;
        public LootDropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropPrototype), proto); }

    }

    public class LootNodePrototype : Prototype
    {
        public LootRollModifierPrototype[] Modifiers;
        public short Weight;
        public LootNodePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootNodePrototype), proto); }

    }

    #region LootRollModifier

    public class LootRollModifierPrototype : Prototype
    {
        public LootRollModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifierPrototype), proto); }
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin;
        public int LevelMax;
        public LootRollClampLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollClampLevelPrototype), proto); }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin;
        public int LevelMax;
        public LootRollRequireLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireLevelPrototype), proto); }
    }

    public class LootRollMarkSpecialPrototype : LootRollModifierPrototype
    {
        public LootRollMarkSpecialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMarkSpecialPrototype), proto); }
    }

    public class LootRollUnmarkSpecialPrototype : LootRollModifierPrototype
    {
        public LootRollUnmarkSpecialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUnmarkSpecialPrototype), proto); }
    }

    public class LootRollMarkRarePrototype : LootRollModifierPrototype
    {
        public LootRollMarkRarePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMarkRarePrototype), proto); }
    }

    public class LootRollUnmarkRarePrototype : LootRollModifierPrototype
    {
        public LootRollUnmarkRarePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUnmarkRarePrototype), proto); }
    }

    public class LootRollOffsetLevelPrototype : LootRollModifierPrototype
    {
        public int LevelOffset;
        public LootRollOffsetLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollOffsetLevelPrototype), proto); }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollOnceDailyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollOnceDailyPrototype), proto); }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollCooldownOncePerRolloverPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollCooldownOncePerRolloverPrototype), proto); }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollCooldownByChannelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollCooldownByChannelPrototype), proto); }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public ulong Avatar;
        public LootRollSetAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetAvatarPrototype), proto); }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level;
        public LootRollSetItemLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetItemLevelPrototype), proto); }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public ulong Category;
        public PositionType Position;
        public short ModifyMinBy;
        public short ModifyMaxBy;
        public LootRollModifyAffixLimitsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifyAffixLimitsPrototype), proto); }
    }

    public enum PositionType {
	    None = 0,
	    Prefix = 1,
	    Suffix = 2,
	    Visual = 3,
	    Cosmic = 5,
	    Unique = 6,
	    Ultimate = 4,
	    Blessing = 7,
	    Runeword = 8,
	    TeamUp = 9,
	    Metadata = 10,
	    PetTech1 = 11,
	    PetTech2 = 12,
	    PetTech3 = 13,
	    PetTech4 = 14,
	    PetTech5 = 15,
	    RegionAffix = 16,
	    Socket1 = 17,
	    Socket2 = 18,
	    Socket3 = 19,
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollSetRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetRarityPrototype), proto); }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable;
        public LootRollSetUsablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetUsablePrototype), proto); }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim;
        public LootRollUseLevelVerbatimPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUseLevelVerbatimPrototype), proto); }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireDifficultyTierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireDifficultyTierPrototype), proto); }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong ModifierCurve;
        public LootRollModifyDropByDifficultyTierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifyDropByDifficultyTierPrototype), proto); }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireConditionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireConditionKeywordPrototype), proto); }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollForbidConditionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidConditionKeywordPrototype), proto); }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireDropperKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireDropperKeywordPrototype), proto); }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollForbidDropperKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidDropperKeywordPrototype), proto); }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireRegionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireRegionKeywordPrototype), proto); }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollForbidRegionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidRegionKeywordPrototype), proto); }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireRegionScenarioRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireRegionScenarioRarityPrototype), proto); }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired;
        public LootRollRequireKillCountPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireKillCountPrototype), proto); }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public ulong Choices;
        public LootRollRequireWeekdayPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireWeekdayPrototype), proto); }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {

        public LootRollIgnoreCooldownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIgnoreCooldownPrototype), proto); }
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
        public LootRollIgnoreVendorXPCapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIgnoreVendorXPCapPrototype), proto); }
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public ulong RegionAffixTable;
        public LootRollSetRegionAffixTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetRegionAffixTablePrototype), proto); }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
        public LootRollIncludeCurrencyBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIncludeCurrencyBonusPrototype), proto); }
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public ulong Missions;
        public int RequiredState;
        public LootRollMissionStateRequiredPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMissionStateRequiredPrototype), proto); }
    }

    #endregion
}
