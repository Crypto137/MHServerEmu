namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public ulong Keyword { get; private set; }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public ulong Avatar { get; private set; }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public ulong Superteam { get; private set; }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public ulong Costume { get; private set; }
    }

    public class PartyFilterPrototype : Prototype
    {
        public bool AllowOutsiders { get; private set; }
        public bool AllUniqueAvatars { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public int NumberRequired { get; private set; }
        public PartyFilterRulePrototype[] Rules { get; private set; }
    }

    public class PublicEventPrototype : Prototype
    {
        public bool DefaultEnabled { get; private set; }
        public ulong Name { get; private set; }
        public ulong[] Teams { get; private set; }
        public ulong PanelName { get; private set; }
    }
}
