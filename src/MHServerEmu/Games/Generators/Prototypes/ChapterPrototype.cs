using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class ChapterPrototype : Prototype
    {
        public ulong ChapterName;
        public int ChapterNumber;
        public ulong ChapterTooltip;
        public bool IsDevOnly;
        public ulong HubWaypoint;
        public bool ShowInShippingUI;
        public ulong Description;
        public bool ResetsOnStoryWarp;
        public bool ShowInUI;
        public bool StartLocked;
        public ulong ChapterEndMission;
        public ulong MapDescription;
        public ulong MapImage;
        public int RecommendedLevelMax;
        public int RecommendedLevelMin;
        public ulong MapImageConsole;
        public ulong LocationImageConsole;
        public ulong ConsoleDescription;
        public ChapterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ChapterPrototype), proto); }
    }

    public class StoryWarpPrototype : Prototype
    {
        public ulong Chapter;
        public ulong Waypoint;
        public StoryWarpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StoryWarpPrototype), proto); }
    }
}
