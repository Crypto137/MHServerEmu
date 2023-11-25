using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GeneratorPrototype : Prototype
    {
    }

    public class DistrictAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong District { get; set; }
    }

    public class AreaGenerationInterfacePrototype : GeneratorPrototype
    {
    }

    public class SingleCellAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong Cell { get; set; }
        public int BorderWidth { get; set; }
        public CellSetEntryPrototype[] BorderCellSets { get; set; }
    }

    public class CellSetEntryPrototype : Prototype
    {
        public ulong CellSet { get; set; }
        public int Weight { get; set; }
        public bool Unique { get; set; }
        public IgnoreOfTypeEntryPrototype[] IgnoreOfType { get; set; }
    }

    public class IgnoreOfTypeEntryPrototype : Prototype
    {
        public Cell.WallGroup Ignore { get; set; }
    }

    public class RequiredPOIAreaEntryPrototype : Prototype
    {
        public ulong Area { get; set; }
        public int Picks { get; set; }
    }

    public class RequiredPOIGroupPrototype : Prototype
    {
        public RequiredPOIAreaEntryPrototype[] Areas { get; set; }
        public RequiredCellBasePrototype[] RequiredCells { get; set; }
    }

    #region CellGridBehaviorPrototype

    public class CellGridBehaviorPrototype : Prototype
    {
        public ulong BehaviorId { get; set; }
    }

    public class BlacklistCellPrototype : Prototype
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class CellGridBlacklistBehaviorPrototype : CellGridBehaviorPrototype
    {
        public BlacklistCellPrototype[] Blacklist { get; set; }
    }

    public class CellGridBorderBehaviorPrototype : CellGridBehaviorPrototype
    {
        public bool DoBorder { get; set; }
        public int BorderWidth { get; set; }
    }

    public class CellGridRampBehaviorPrototype : CellGridBehaviorPrototype
    {
        public ulong EdgeStart { get; set; }
        public ulong EdgeEnd { get; set; }
        public float Increment { get; set; }
    }

    #endregion

    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X { get; set; }
        public sbyte Y { get; set; }
        public ulong Cell { get; set; }
        public ulong[] Alts { get; set; }
    }

    public class SuperCellPrototype : Prototype
    {
        public SuperCellEntryPrototype[] Entries { get; set; }
    }
}
