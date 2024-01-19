using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Standard)]
    public enum AreaMinimapReveal
    {
        Standard,
        PlayerAreaOnly,
        PlayerCellOnly,
        PlayerAreaGroup,
    }

    #endregion

    public class AreaPrototype : Prototype
    {
        public GeneratorPrototype Generator { get; protected set; }
        public ulong Population { get; protected set; }
        public ulong AreaName { get; protected set; }
        public ulong PropDensity { get; protected set; }
        public ulong[] PropSets { get; protected set; }
        public StyleEntryPrototype[] Styles { get; protected set; }
        public ulong ClientMap { get; protected set; }
        public ulong[] Music { get; protected set; }
        public bool FullyGenerateCells { get; protected set; }
        public AreaMinimapReveal MinimapRevealMode { get; protected set; }
        public ulong AmbientSfx { get; protected set; }
        public ulong MinimapName { get; protected set; }
        public int MinimapRevealGroupId { get; protected set; }
        public ulong RespawnOverride { get; protected set; }
        public ulong PlayerCameraSettings { get; protected set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; protected set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; protected set; }
        public ulong[] Keywords { get; protected set; }
        public int LevelOffset { get; protected set; }
        public RespawnCellOverridePrototype[] RespawnCellOverrides { get; protected set; }
        public ulong PlayerCameraSettingsOrbis { get; protected set; }
    }

    public class AreaTransitionPrototype : Prototype
    {
        public ulong Type { get; protected set; }
    }

    public class RespawnCellOverridePrototype : Prototype
    {
        public ulong[] Cells { get; protected set; }
        public ulong RespawnOverride { get; protected set; }
    }

    public class StyleEntryPrototype : Prototype
    {
        public ulong Population { get; protected set; }
        public ulong[] PropSets { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class AreaListPrototype : Prototype
    {
        public ulong[] Areas { get; protected set; }
    }
}
