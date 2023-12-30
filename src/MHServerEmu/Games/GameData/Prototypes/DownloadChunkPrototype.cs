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
        public RegionPrototype Regions { get; protected set; }
        public Platforms Platform { get; protected set; }
    }

    public class DownloadChunkPrototype : Prototype
    {
        public ulong Chapter { get; protected set; }
        public ulong[] Data { get; protected set; }
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform { get; protected set; }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public DownloadChunkPrototype ChunksPC { get; protected set; }
        public DownloadChunkPrototype ChunksPS4 { get; protected set; }
        public DownloadChunkPrototype ChunksXboxOne { get; protected set; }
    }
}
