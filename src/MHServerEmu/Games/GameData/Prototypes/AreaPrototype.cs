namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum AreaMinimapReveal
    {
        Standard,
        PlayerAreaOnly,
        PlayerCellOnly,
        PlayerAreaGroup,
    }

    public class AreaPrototype : Prototype
    {
        public GeneratorPrototype Generator { get; set; }
        public ulong Population { get; set; }
        public ulong AreaName { get; set; }
        public ulong PropDensity { get; set; }
        public ulong[] PropSets { get; set; }
        public StyleEntryPrototype[] Styles { get; set; }
        public ulong ClientMap { get; set; }
        public ulong[] Music { get; set; }
        public bool FullyGenerateCells { get; set; }
        public AreaMinimapReveal MinimapRevealMode { get; set; }
        public ulong AmbientSfx { get; set; }
        public ulong MinimapName { get; set; }
        public int MinimapRevealGroupId { get; set; }
        public ulong RespawnOverride { get; set; }
        public ulong PlayerCameraSettings { get; set; }
        public FootstepTraceBehaviorAsset FootstepTraceOverride { get; set; }
        public RegionMusicBehaviorAsset MusicBehavior { get; set; }
        public ulong[] Keywords { get; set; }
        public int LevelOffset { get; set; }
        public RespawnCellOverridePrototype[] RespawnCellOverrides { get; set; }
        public ulong PlayerCameraSettingsOrbis { get; set; }
    }

    public class AreaTransitionPrototype : Prototype
    {
        public ulong Type { get; set; }
    }

    public class RespawnCellOverridePrototype : Prototype
    {
        public ulong[] Cells { get; set; }
        public ulong RespawnOverride { get; set; }
    }

    public class StyleEntryPrototype : Prototype
    {
        public ulong Population { get; set; }
        public ulong[] PropSets { get; set; }
        public int Weight { get; set; }
    }

    public class AreaListPrototype : Prototype
    {
        public ulong[] Areas { get; set; }
    }
}
