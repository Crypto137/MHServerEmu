using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    [Flags]
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

    #endregion

    public class DownloadChunkRegionsPrototype : Prototype
    {
        public RegionPrototype Regions { get; private set; }
        public Platforms Platform { get; private set; }
    }

    public class DownloadChunkPrototype : Prototype
    {
        public ulong Chapter { get; private set; }
        public ulong[] Data { get; private set; }
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform { get; private set; }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public DownloadChunkPrototype ChunksPC { get; private set; }
        public DownloadChunkPrototype ChunksPS4 { get; private set; }
        public DownloadChunkPrototype ChunksXboxOne { get; private set; }
    }
}
