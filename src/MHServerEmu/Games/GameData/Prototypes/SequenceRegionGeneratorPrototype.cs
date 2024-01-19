using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)NoRestriction)]
    [Flags]
    public enum RegionDirection
    {
        NoRestriction = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8,
        NorthSouth = 5,
        EastWest = 10,
    }

    #endregion

    public class SequenceRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public ulong RegionPOIPicker { get; protected set; }
        public int EndlessLevelsPerTheme { get; protected set; }
        public EndlessThemePrototype[] EndlessThemes { get; protected set; }
        public SubGenerationPrototype[] SubAreaSequences { get; protected set; }
    }

    public class SubGenerationPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public float MinRootSeparation { get; protected set; }
        public int Tries { get; protected set; }
    }

    public class EndlessThemePrototype : Prototype
    {
        public EndlessThemeEntryPrototype Boss { get; protected set; }
        public EndlessThemeEntryPrototype Normal { get; protected set; }
        public EndlessThemeEntryPrototype TreasureRoom { get; protected set; }
    }

    public class EndlessThemeEntryPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; protected set; }
        public EndlessStateEntryPrototype[] Challenges { get; protected set; }
    }

    public class EndlessStateEntryPrototype : Prototype
    {
        public ulong MetaState { get; protected set; }
        public ulong RegionPOIPicker { get; protected set; }
        public MetaStateChallengeTierEnum Tier { get; protected set; }
    }

    public class AreaSequenceInfoPrototype : Prototype
    {
        public WeightedAreaPrototype[] AreaChoices { get; protected set; }
        public AreaSequenceInfoPrototype[] ConnectedTo { get; protected set; }
        public short ConnectedToPicks { get; protected set; }
        public bool ConnectAllShared { get; protected set; }
        public short SharedEdgeMinimum { get; protected set; }
        public short Weight { get; protected set; }
    }

    public class WeightedAreaPrototype : Prototype
    {
        public ulong Area { get; protected set; }
        public int Weight { get; protected set; }
        public RegionDirection ConnectOn { get; protected set; }
        public ulong RespawnOverride { get; protected set; }
        public bool AlignedToPrevious { get; protected set; }
    }
}
