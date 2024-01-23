namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ChapterPrototype : Prototype
    {
        public LocaleStringId ChapterName { get; protected set; }
        public int ChapterNumber { get; protected set; }
        public LocaleStringId ChapterTooltip { get; protected set; }
        public bool IsDevOnly { get; protected set; }
        public PrototypeId HubWaypoint { get; protected set; }
        public bool ShowInShippingUI { get; protected set; }
        public LocaleStringId Description { get; protected set; }
        public bool ResetsOnStoryWarp { get; protected set; }
        public bool ShowInUI { get; protected set; }
        public bool StartLocked { get; protected set; }
        public PrototypeId ChapterEndMission { get; protected set; }
        public LocaleStringId MapDescription { get; protected set; }
        public StringId MapImage { get; protected set; }
        public int RecommendedLevelMax { get; protected set; }
        public int RecommendedLevelMin { get; protected set; }
        public StringId MapImageConsole { get; protected set; }
        public StringId LocationImageConsole { get; protected set; }
        public LocaleStringId ConsoleDescription { get; protected set; }
    }

    public class StoryWarpPrototype : Prototype
    {
        public PrototypeId Chapter { get; protected set; }
        public PrototypeId Waypoint { get; protected set; }
    }
}
