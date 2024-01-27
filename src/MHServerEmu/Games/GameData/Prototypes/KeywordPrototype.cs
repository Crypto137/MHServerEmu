namespace MHServerEmu.Games.GameData.Prototypes
{
    public class KeywordPrototype : Prototype
    {
        public PrototypeId IsAKeyword { get; protected set; }
    }

    public class EntityKeywordPrototype : KeywordPrototype
    {
        public LocaleStringId DisplayName { get; protected set; }
    }

    public class MobKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class AvatarKeywordPrototype : EntityKeywordPrototype
    {
    }

    public class MissionKeywordPrototype : KeywordPrototype
    {
    }

    public class PowerKeywordPrototype : KeywordPrototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public bool DisplayInPowerKeywordsList { get; protected set; }
    }

    public class RankKeywordPrototype : KeywordPrototype
    {
    }

    public class RegionKeywordPrototype : KeywordPrototype
    {
    }

    public class AffixCategoryPrototype : KeywordPrototype
    {
    }

    public class FulfillablePrototype : Prototype
    {
    }

}
