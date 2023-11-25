namespace MHServerEmu.Games.GameData.Prototypes
{
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

    public class SequenceRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; set; }
        public ulong RegionPOIPicker { get; set; }
        public int EndlessLevelsPerTheme { get; set; }
        public EndlessThemePrototype[] EndlessThemes { get; set; }
        public SubGenerationPrototype[] SubAreaSequences { get; set; }
    }

    public class SubGenerationPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; set; }
        public float MinRootSeparation { get; set; }
        public int Tries { get; set; }
    }

    public class EndlessThemePrototype : Prototype
    {
        public EndlessThemeEntryPrototype Boss { get; set; }
        public EndlessThemeEntryPrototype Normal { get; set; }
        public EndlessThemeEntryPrototype TreasureRoom { get; set; }
    }

    public class EndlessThemeEntryPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; set; }
        public EndlessStateEntryPrototype[] Challenges { get; set; }
    }

    public class EndlessStateEntryPrototype : Prototype
    {
        public ulong MetaState { get; set; }
        public ulong RegionPOIPicker { get; set; }
        public MetaStateChallengeTierEnum Tier { get; set; }
    }

    public class AreaSequenceInfoPrototype : Prototype
    {
        public WeightedAreaPrototype[] AreaChoices { get; set; }
        public AreaSequenceInfoPrototype[] ConnectedTo { get; set; }
        public short ConnectedToPicks { get; set; }
        public bool ConnectAllShared { get; set; }
        public short SharedEdgeMinimum { get; set; }
        public short Weight { get; set; }
    }

    public class WeightedAreaPrototype : Prototype
    {
        public ulong Area { get; set; }
        public int Weight { get; set; }
        public RegionDirection ConnectOn { get; set; }
        public ulong RespawnOverride { get; set; }
        public bool AlignedToPrevious { get; set; }
    }
}
