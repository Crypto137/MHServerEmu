using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
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
        public GeneratorPrototype Generator { get; private set; }
        public ulong Population { get; private set; }
        public ulong AreaName { get; private set; }
        public ulong PropDensity { get; private set; }
        public ulong[] PropSets { get; private set; }
        public StyleEntryPrototype[] Styles { get; private set; }
        public ulong ClientMap { get; private set; }
        public ulong[] Music { get; private set; }
        public bool FullyGenerateCells { get; private set; }
        public AreaMinimapReveal MinimapRevealMode { get; private set; }
        public ulong AmbientSfx { get; private set; }
        public ulong MinimapName { get; private set; }
        public int MinimapRevealGroupId { get; private set; }
        public ulong RespawnOverride { get; private set; }
        public ulong PlayerCameraSettings { get; private set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; private set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; private set; }
        public ulong[] Keywords { get; private set; }
        public int LevelOffset { get; private set; }
        public RespawnCellOverridePrototype[] RespawnCellOverrides { get; private set; }
        public ulong PlayerCameraSettingsOrbis { get; private set; }
    }

    public class AreaTransitionPrototype : Prototype
    {
        public ulong Type { get; private set; }
    }

    public class RespawnCellOverridePrototype : Prototype
    {
        public ulong[] Cells { get; private set; }
        public ulong RespawnOverride { get; private set; }
    }

    public class StyleEntryPrototype : Prototype
    {
        public ulong Population { get; private set; }
        public ulong[] PropSets { get; private set; }
        public int Weight { get; private set; }
    }

    public class AreaListPrototype : Prototype
    {
        public ulong[] Areas { get; private set; }
    }
}
