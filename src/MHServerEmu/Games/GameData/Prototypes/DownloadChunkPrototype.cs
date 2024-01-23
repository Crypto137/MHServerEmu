using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)All)]
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
        public PrototypeId[] Regions { get; protected set; }  // VectorPrototypeRefPtr RegionPrototype
        public Platforms Platform { get; protected set; }
    }

    public class DownloadChunkPrototype : Prototype
    {
        public PrototypeId Chapter { get; protected set; }
        public StringId[] Data { get; protected set; }
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform { get; protected set; }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public PrototypeId[] ChunksPC { get; protected set; }      // VectorPrototypeRefPtr DownloadChunkPrototype
        public PrototypeId[] ChunksPS4 { get; protected set; }     // VectorPrototypeRefPtr DownloadChunkPrototype
        public PrototypeId[] ChunksXboxOne { get; protected set; } // VectorPrototypeRefPtr DownloadChunkPrototype
    }
}
