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

    // There isn't much point to using VectorPrototypeRefPtr for these server-side,
    // but it's what the client does, so whatever. Authenticity FTW.

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
    public class DownloadChunkRegionsPrototype : Prototype
    {
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public RegionPrototype[] Regions { get; protected set; }
        public Platforms Platform { get; protected set; }
    }
#endif

    public class DownloadChunkPrototype : Prototype
    {
#if GAME_VERSION_1_48
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public RegionPrototype[] Regions { get; protected set; }
#endif
        public PrototypeId Chapter { get; protected set; }
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public AssetId[] Data { get; protected set; }
        public DownloadChunkRegionsPrototype[] RegionsPerPlatform { get; protected set; }
#endif
    }

    public class DownloadChunksPrototype : Prototype
    {
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public DownloadChunkPrototype[] ChunksPC { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public DownloadChunkPrototype[] ChunksPS4 { get; protected set; }
        [PrototypeField(PrototypeFieldType.VectorPrototypeRefPtr)]
        public DownloadChunkPrototype[] ChunksXboxOne { get; protected set; }
#else
        public PrototypeId[] Chunks { get; protected set; }
#endif
    }
}
