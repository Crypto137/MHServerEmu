using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class DownloadChunkRegionsPrototype : Prototype
    {
        public RegionPrototype Regions;
        public Platforms Platform;
        public DownloadChunkRegionsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DownloadChunkRegionsPrototype), proto); }
    }
    public enum Platforms
    {
        None = 0,
        PC_DEPRECATED = 1,
        Console = 6,
        PC = 8,
        PS4 = 2,
        XboxOne = 4,
        All = 15,
    }
    public class DownloadChunkPrototype : Prototype
    {
        public ulong Chapter;
        public ulong[] Data;
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform;
        public DownloadChunkPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DownloadChunkPrototype), proto); }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public DownloadChunkPrototype ChunksPC;
        public DownloadChunkPrototype ChunksPS4;
        public DownloadChunkPrototype ChunksXboxOne;
        public DownloadChunksPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DownloadChunksPrototype), proto); }
    }
}
