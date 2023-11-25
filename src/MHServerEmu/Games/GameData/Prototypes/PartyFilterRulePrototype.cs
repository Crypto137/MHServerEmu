namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PartyFilterRulePrototype : Prototype
    {
    }

    public class PartyFilterRuleHasKeywordPrototype : PartyFilterRulePrototype
    {
        public ulong Keyword { get; set; }
    }

    public class PartyFilterRuleHasPrototypePrototype : PartyFilterRulePrototype
    {
        public ulong Avatar { get; set; }
    }

    public class PartyFilterRuleMemberOfTeamPrototype : PartyFilterRulePrototype
    {
        public ulong Superteam { get; set; }
    }

    public class PartyFilterRuleWearingCostumePrototype : PartyFilterRulePrototype
    {
        public ulong Costume { get; set; }
    }

    public class PartyFilterPrototype : Prototype
    {
        public bool AllowOutsiders { get; set; }
        public bool AllUniqueAvatars { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public int NumberRequired { get; set; }
        public PartyFilterRulePrototype[] Rules { get; set; }
    }

    public class PublicEventPrototype : Prototype
    {
        public bool DefaultEnabled { get; set; }
        public ulong Name { get; set; }
        public ulong[] Teams { get; set; }
        public ulong PanelName { get; set; }
    }
}
