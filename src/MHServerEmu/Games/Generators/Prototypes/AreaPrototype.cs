using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class AreaPrototype : Prototype
    {
        public GeneratorPrototype Generator;
        public int LevelOffset;
        public ulong Population;
        public ulong AreaName;
        public ulong PropDensity;
        public ulong[] PropSets;
        public StyleEntryPrototype[] Styles;
        public ulong ClientMap;
        public ulong AmbientSfx;
        public ulong[] Music;
        public RegionMusicBehavior MusicBehavior;
        public FootstepTrace FootstepTraceOverride;
        public bool FullyGenerateCells;
        public AreaMinimapReveal MinimapRevealMode;
        public int MinimapRevealGroupId;
        public ulong MinimapName;
        public ulong RespawnOverride;
        public RespawnCellOverridePrototype[] RespawnCellOverrides;
        public ulong PlayerCameraSettings;
        public ulong PlayerCameraSettingsOrbis;
        public ulong[] Keywords;

        public AreaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaPrototype), proto); }
    }

    public enum AreaMinimapReveal {
	    Standard,
	    PlayerAreaOnly,
	    PlayerCellOnly,
	    PlayerAreaGroup,
    }

    public class RespawnCellOverridePrototype : Prototype
    {
        public ulong[] Cells;
        public ulong RespawnOverride;

        public RespawnCellOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RespawnCellOverridePrototype), proto); }
    }

    public class StyleEntryPrototype : Prototype
    {
        public ulong Population;
        public ulong[] PropSets;
        public int Weight;

        public StyleEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StyleEntryPrototype), proto); }
    }
}
