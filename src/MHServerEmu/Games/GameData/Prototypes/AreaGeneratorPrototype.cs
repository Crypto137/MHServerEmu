using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GeneratorPrototype : Prototype
    {
    }

    public class DistrictAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong District { get; private set; }
    }

    public class AreaGenerationInterfacePrototype : GeneratorPrototype
    {
    }

    public class SingleCellAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong Cell { get; private set; }
        public int BorderWidth { get; private set; }
        public CellSetEntryPrototype[] BorderCellSets { get; private set; }
    }

    public class CellSetEntryPrototype : Prototype
    {
        public ulong CellSet { get; private set; }
        public int Weight { get; private set; }
        public bool Unique { get; private set; }
        public IgnoreOfTypeEntryPrototype[] IgnoreOfType { get; private set; }
    }

    public class IgnoreOfTypeEntryPrototype : Prototype
    {
        public Cell.WallGroup Ignore { get; private set; }
    }

    public class RequiredPOIAreaEntryPrototype : Prototype
    {
        public ulong Area { get; private set; }
        public int Picks { get; private set; }
    }

    public class RequiredPOIGroupPrototype : Prototype
    {
        public RequiredPOIAreaEntryPrototype[] Areas { get; private set; }
        public RequiredCellBasePrototype[] RequiredCells { get; private set; }
    }

    #region CellGridBehaviorPrototype

    public class CellGridBehaviorPrototype : Prototype
    {
        public ulong BehaviorId { get; private set; }
    }

    public class BlacklistCellPrototype : Prototype
    {
        public int X { get; private set; }
        public int Y { get; private set; }
    }

    public class CellGridBlacklistBehaviorPrototype : CellGridBehaviorPrototype
    {
        public BlacklistCellPrototype[] Blacklist { get; private set; }
    }

    public class CellGridBorderBehaviorPrototype : CellGridBehaviorPrototype
    {
        public bool DoBorder { get; private set; }
        public int BorderWidth { get; private set; }
    }

    public class CellGridRampBehaviorPrototype : CellGridBehaviorPrototype
    {
        public ulong EdgeStart { get; private set; }
        public ulong EdgeEnd { get; private set; }
        public float Increment { get; private set; }
    }

    #endregion

    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X { get; private set; }
        public sbyte Y { get; private set; }
        public ulong Cell { get; private set; }
        public ulong[] Alts { get; private set; }
    }

    public class SuperCellPrototype : Prototype
    {
        public SuperCellEntryPrototype[] Entries { get; private set; }
    }
}
