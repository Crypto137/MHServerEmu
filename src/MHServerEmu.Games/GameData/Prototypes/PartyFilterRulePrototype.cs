using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public PrototypeId Keyword { get; protected set; }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public PrototypeId Superteam { get; protected set; }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public PrototypeId Costume { get; protected set; }
    }

    public class PartyFilterPrototype : Prototype
    {
        public bool AllowOutsiders { get; protected set; }
        public bool AllUniqueAvatars { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public int NumberRequired { get; protected set; }
        public PartyFilterRulePrototype[] Rules { get; protected set; }
    }

    public class PublicEventPrototype : Prototype
    {
        public bool DefaultEnabled { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public PrototypeId[] Teams { get; protected set; }
        public AssetId PanelName { get; protected set; }

        [DoNotCopy]
        public int PublicEventPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();
            PublicEventPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetPublicEventBlueprintDataRef());
        }

    }
}
