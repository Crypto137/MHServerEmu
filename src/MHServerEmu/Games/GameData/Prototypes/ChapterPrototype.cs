namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ChapterPrototype : Prototype
    {
        public ulong ChapterName { get; protected set; }
        public int ChapterNumber { get; protected set; }
        public ulong ChapterTooltip { get; protected set; }
        public bool IsDevOnly { get; protected set; }
        public ulong HubWaypoint { get; protected set; }
        public bool ShowInShippingUI { get; protected set; }
        public ulong Description { get; protected set; }
        public bool ResetsOnStoryWarp { get; protected set; }
        public bool ShowInUI { get; protected set; }
        public bool StartLocked { get; protected set; }
        public ulong ChapterEndMission { get; protected set; }
        public ulong MapDescription { get; protected set; }
        public ulong MapImage { get; protected set; }
        public int RecommendedLevelMax { get; protected set; }
        public int RecommendedLevelMin { get; protected set; }
        public ulong MapImageConsole { get; protected set; }
        public ulong LocationImageConsole { get; protected set; }
        public ulong ConsoleDescription { get; protected set; }
    }

    public class StoryWarpPrototype : Prototype
    {
        public ulong Chapter { get; protected set; }
        public ulong Waypoint { get; protected set; }
    }
}
