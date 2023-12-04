using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class AreaPrototype : Prototype
    {
        public GeneratorPrototype Generator;
        public ulong Population;
        public ulong AreaName;
        public ulong PropDensity;
        public ulong[] PropSets;
        public StyleEntryPrototype[] Styles;
        public ulong ClientMap;
        public ulong[] Music;
        public bool FullyGenerateCells;
        public AreaMinimapReveal MinimapRevealMode;
        public ulong AmbientSfx;
        public ulong MinimapName;
        public int MinimapRevealGroupId;
        public ulong RespawnOverride;
        public ulong PlayerCameraSettings;
        public FootstepTraceBehaviorAsset FootstepTraceOverride;
        public RegionMusicBehaviorAsset MusicBehavior;
        public ulong[] Keywords;
        public int LevelOffset;
        public RespawnCellOverridePrototype[] RespawnCellOverrides;
        public ulong PlayerCameraSettingsOrbis;
        public AreaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaPrototype), proto); }
    }

    public class AreaTransition
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public AreaTransitionPrototype Prototype;
    }

    public class AreaTransitionPrototype : Prototype
    {
        public ulong Type;
        public AreaTransitionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaTransitionPrototype), proto); }
    };

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

    public class AreaListPrototype : Prototype
    {
        public ulong[] Areas;
        public AreaListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaListPrototype), proto); }
    }
}
