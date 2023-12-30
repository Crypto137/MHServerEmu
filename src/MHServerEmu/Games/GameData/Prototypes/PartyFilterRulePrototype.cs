namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public ulong Keyword { get; protected set; }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public ulong Avatar { get; protected set; }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public ulong Superteam { get; protected set; }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public ulong Costume { get; protected set; }
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
        public ulong Name { get; protected set; }
        public ulong[] Teams { get; protected set; }
        public ulong PanelName { get; protected set; }
    }
}
