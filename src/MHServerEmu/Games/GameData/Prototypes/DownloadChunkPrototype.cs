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
        public ulong[] Regions { get; protected set; }  // VectorPrototypeRefPtr RegionPrototype
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
        public ulong[] ChunksPC { get; protected set; }      // VectorPrototypeRefPtr DownloadChunkPrototype
        public ulong[] ChunksPS4 { get; protected set; }     // VectorPrototypeRefPtr DownloadChunkPrototype
        public ulong[] ChunksXboxOne { get; protected set; } // VectorPrototypeRefPtr DownloadChunkPrototype
    }
}
