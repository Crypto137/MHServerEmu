using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
        public PartyFilterRulePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterRulePrototype), proto); }
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public ulong Keyword;
        public PartyFilterRuleHasKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterRuleHasKeywordPrototype), proto); }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public ulong Avatar;
        public PartyFilterRuleHasPrototypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterRuleHasPrototypePrototype), proto); }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public ulong Superteam;
        public PartyFilterRuleMemberOfTeamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterRuleMemberOfTeamPrototype), proto); }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public ulong Costume;
        public PartyFilterRuleWearingCostumePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterRuleWearingCostumePrototype), proto); }
    }

    public class PartyFilterPrototype : Prototype
    {
        public bool AllowOutsiders;
        public bool AllUniqueAvatars;
        public DesignWorkflowState DesignState;
        public int NumberRequired;
        public PartyFilterRulePrototype[] Rules;
        public PartyFilterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PartyFilterPrototype), proto); }
    }

    public class PublicEventPrototype : Prototype
    {
        public bool DefaultEnabled;
        public ulong Name;
        public ulong[] Teams;
        public ulong PanelName;
        public PublicEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PublicEventPrototype), proto); }
    }
}
