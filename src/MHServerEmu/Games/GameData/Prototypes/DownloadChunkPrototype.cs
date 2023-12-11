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
        public RegionPrototype Regions { get; set; }
        public Platforms Platform { get; set; }
    }

    public class DownloadChunkPrototype : Prototype
    {
        public ulong Chapter { get; set; }
        public ulong[] Data { get; set; }
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform { get; set; }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public DownloadChunkPrototype ChunksPC { get; set; }
        public DownloadChunkPrototype ChunksPS4 { get; set; }
        public DownloadChunkPrototype ChunksXboxOne { get; set; }
    }
}
