namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ChapterPrototype : Prototype
    {
        public ulong ChapterName { get; private set; }
        public int ChapterNumber { get; private set; }
        public ulong ChapterTooltip { get; private set; }
        public bool IsDevOnly { get; private set; }
        public ulong HubWaypoint { get; private set; }
        public bool ShowInShippingUI { get; private set; }
        public ulong Description { get; private set; }
        public bool ResetsOnStoryWarp { get; private set; }
        public bool ShowInUI { get; private set; }
        public bool StartLocked { get; private set; }
        public ulong ChapterEndMission { get; private set; }
        public ulong MapDescription { get; private set; }
        public ulong MapImage { get; private set; }
        public int RecommendedLevelMax { get; private set; }
        public int RecommendedLevelMin { get; private set; }
        public ulong MapImageConsole { get; private set; }
        public ulong LocationImageConsole { get; private set; }
        public ulong ConsoleDescription { get; private set; }
    }

    public class StoryWarpPrototype : Prototype
    {
        public ulong Chapter { get; private set; }
        public ulong Waypoint { get; private set; }
    }
}
