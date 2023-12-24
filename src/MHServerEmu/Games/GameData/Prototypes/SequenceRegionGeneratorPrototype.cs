using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
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
        public AreaSequenceInfoPrototype[] AreaSequence { get; private set; }
        public ulong RegionPOIPicker { get; private set; }
        public int EndlessLevelsPerTheme { get; private set; }
        public EndlessThemePrototype[] EndlessThemes { get; private set; }
        public SubGenerationPrototype[] SubAreaSequences { get; private set; }
    }

    public class SubGenerationPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; private set; }
        public float MinRootSeparation { get; private set; }
        public int Tries { get; private set; }
    }

    public class EndlessThemePrototype : Prototype
    {
        public EndlessThemeEntryPrototype Boss { get; private set; }
        public EndlessThemeEntryPrototype Normal { get; private set; }
        public EndlessThemeEntryPrototype TreasureRoom { get; private set; }
    }

    public class EndlessThemeEntryPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence { get; private set; }
        public EndlessStateEntryPrototype[] Challenges { get; private set; }
    }

    public class EndlessStateEntryPrototype : Prototype
    {
        public ulong MetaState { get; private set; }
        public ulong RegionPOIPicker { get; private set; }
        public MetaStateChallengeTierEnum Tier { get; private set; }
    }

    public class AreaSequenceInfoPrototype : Prototype
    {
        public WeightedAreaPrototype[] AreaChoices { get; private set; }
        public AreaSequenceInfoPrototype[] ConnectedTo { get; private set; }
        public short ConnectedToPicks { get; private set; }
        public bool ConnectAllShared { get; private set; }
        public short SharedEdgeMinimum { get; private set; }
        public short Weight { get; private set; }
    }

    public class WeightedAreaPrototype : Prototype
    {
        public ulong Area { get; private set; }
        public int Weight { get; private set; }
        public RegionDirection ConnectOn { get; private set; }
        public ulong RespawnOverride { get; private set; }
        public bool AlignedToPrevious { get; private set; }
    }
}
