using MHServerEmu.Games.GameData.Calligraphy.Attributes;

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

    public class DownloadChunkPrototype : Prototype
    {
        public PrototypeId[] Regions { get; protected set; }
        public PrototypeId Chapter { get; protected set; }
    }

    public class DownloadChunksPrototype : Prototype
    {
        public PrototypeId[] Chunks { get; protected set; }
    }
}
