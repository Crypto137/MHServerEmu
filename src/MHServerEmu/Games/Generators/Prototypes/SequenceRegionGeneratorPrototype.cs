using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class SequenceRegionGeneratorPrototype : RegionGeneratorPrototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence;
        public ulong RegionPOIPicker;
        public int EndlessLevelsPerTheme;
        public EndlessThemePrototype[] EndlessThemes;
        public SubGenerationPrototype[] SubAreaSequences;
        public SequenceRegionGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SequenceRegionGeneratorPrototype), proto); }
    }

    public class SubGenerationPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence;
        public float MinRootSeparation;
        public int Tries;
        public SubGenerationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SubGenerationPrototype), proto); }
    }

    public class EndlessThemePrototype : Prototype
    {
        public EndlessThemeEntryPrototype Boss;
        public EndlessThemeEntryPrototype Normal;
        public EndlessThemeEntryPrototype TreasureRoom;
        public EndlessThemePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EndlessThemePrototype), proto); }
    }

    public class EndlessThemeEntryPrototype : Prototype
    {
        public AreaSequenceInfoPrototype[] AreaSequence;
        public EndlessStateEntryPrototype[] Challenges;
        public EndlessThemeEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EndlessThemeEntryPrototype), proto); }
    }

    public class EndlessStateEntryPrototype : Prototype
    {
        public ulong MetaState;
        public ulong RegionPOIPicker;
        public MetaStateChallengeTier Tier;
        public EndlessStateEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EndlessStateEntryPrototype), proto); }
    }

    public enum MetaStateChallengeTier
    {
        None,
        Tier1,
        Tier2,
        Tier3,
        Tier4,
        Tier5,
    }
    public class AreaSequenceInfoPrototype : Prototype
    {
        public WeightedAreaPrototype[] AreaChoices;
        public AreaSequenceInfoPrototype[] ConnectedTo;
        public short ConnectedToPicks;
        public bool ConnectAllShared;
        public short SharedEdgeMinimum;
        public short Weight;
        public AreaSequenceInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AreaSequenceInfoPrototype), proto); }
    }

    public class WeightedAreaPrototype : Prototype
    {
        public ulong Area;
        public int Weight;
        public RegionDirection ConnectOn;
        public ulong RespawnOverride;
        public bool AlignedToPrevious;
        public WeightedAreaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedAreaPrototype), proto); }
    }
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
}
