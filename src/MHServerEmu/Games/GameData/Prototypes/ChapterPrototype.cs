namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ChapterPrototype : Prototype
    {
        public ulong ChapterName { get; set; }
        public int ChapterNumber { get; set; }
        public ulong ChapterTooltip { get; set; }
        public bool IsDevOnly { get; set; }
        public ulong HubWaypoint { get; set; }
        public bool ShowInShippingUI { get; set; }
        public ulong Description { get; set; }
        public bool ResetsOnStoryWarp { get; set; }
        public bool ShowInUI { get; set; }
        public bool StartLocked { get; set; }
        public ulong ChapterEndMission { get; set; }
        public ulong MapDescription { get; set; }
        public ulong MapImage { get; set; }
        public int RecommendedLevelMax { get; set; }
        public int RecommendedLevelMin { get; set; }
        public ulong MapImageConsole { get; set; }
        public ulong LocationImageConsole { get; set; }
        public ulong ConsoleDescription { get; set; }
    }

    public class StoryWarpPrototype : Prototype
    {
        public ulong Chapter { get; set; }
        public ulong Waypoint { get; set; }
    }
}
