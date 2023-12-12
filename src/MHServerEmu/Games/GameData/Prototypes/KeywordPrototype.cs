namespace MHServerEmu.Games.GameData.Prototypes
{
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

}
