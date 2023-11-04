using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class IPoint2Prototype : Prototype
    {
        public int X;
        public int Y;

        public IPoint2Prototype(Prototype proto) : base(proto) { FillPrototype(typeof(IPoint2Prototype), proto); }

    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public ulong Name;
        public LootEventType Event;
        public ulong Table;

        public LootTableAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootTableAssignmentPrototype), proto); }
    }

    public enum LootEventType {
	    None = 0,
	    OnInteractedWith = 3,
	    OnHealthBelowPct = 2,
	    OnHealthBelowPctHit = 1,
	    OnKilled = 4,
	    OnKilledChampion = 5,
	    OnKilledElite = 6,
	    OnKilledMiniBoss = 7,
	    OnHit = 8,
	    OnDamagedForPctHealth = 9,
    }

    public class TransitionUIPrototype : Prototype
    {
        public WeightedTipCategoryPrototype[] TipCategories;
        public TransitionUIType TransitionType;
        public int Weight;

        public TransitionUIPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransitionUIPrototype), proto); }

    }
    public enum TransitionUIType {
	    Environment,
	    HeroOwned,
    }
    public class WeightedTipCategoryPrototype : Prototype
    {

        public TipTypeEnum TipType;
        public int Weight;

        public WeightedTipCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedTipCategoryPrototype), proto); }

    }

    public enum TipTypeEnum {
	    GenericGameplay,
	    SpecificGameplay,
    }


    #region KeywordPrototype

    public class KeywordPrototype : Prototype
    {
        public ulong IsAKeyword;
        public KeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(KeywordPrototype), proto); }
    }

    public class EntityKeywordPrototype : KeywordPrototype
    {
        public ulong DisplayName;
        public EntityKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityKeywordPrototype), proto); }
    }

    public class MobKeywordPrototype : EntityKeywordPrototype
    {
        public MobKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MobKeywordPrototype), proto); }
    }

    public class AvatarKeywordPrototype : EntityKeywordPrototype
    {
        public AvatarKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarKeywordPrototype), proto); }
    }

    public class MissionKeywordPrototype : KeywordPrototype
    {
        public MissionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionKeywordPrototype), proto); }
    }

    public class PowerKeywordPrototype : KeywordPrototype
    {
        public ulong DisplayName;
        public PowerKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerKeywordPrototype), proto); }
    }

    public class RankKeywordPrototype : KeywordPrototype
    {
        public RankKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankKeywordPrototype), proto); }
    }

    public class RegionKeywordPrototype : KeywordPrototype
    {
        public RegionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RegionKeywordPrototype), proto); }
    }

    public class AffixCategoryPrototype : KeywordPrototype
    {
        public AffixCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixCategoryPrototype), proto); }
    }
    #endregion
}
