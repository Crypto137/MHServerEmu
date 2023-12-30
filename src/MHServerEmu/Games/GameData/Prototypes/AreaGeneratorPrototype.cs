using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GeneratorPrototype : Prototype
    {
    }

    public class DistrictAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong District { get; protected set; }
    }

    public class AreaGenerationInterfacePrototype : GeneratorPrototype
    {
    }

    public class SingleCellAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong Cell { get; protected set; }
        public int BorderWidth { get; protected set; }
        public CellSetEntryPrototype[] BorderCellSets { get; protected set; }
    }

    public class CellSetEntryPrototype : Prototype
    {
        public ulong CellSet { get; protected set; }
        public int Weight { get; protected set; }
        public bool Unique { get; protected set; }
        public IgnoreOfTypeEntryPrototype[] IgnoreOfType { get; protected set; }
    }

    public class IgnoreOfTypeEntryPrototype : Prototype
    {
        public Cell.WallGroup Ignore { get; protected set; }
    }

    public class RequiredPOIAreaEntryPrototype : Prototype
    {
        public ulong Area { get; protected set; }
        public int Picks { get; protected set; }
    }

    public class RequiredPOIGroupPrototype : Prototype
    {
        public RequiredPOIAreaEntryPrototype[] Areas { get; protected set; }
        public RequiredCellBasePrototype[] RequiredCells { get; protected set; }
    }

    #region CellGridBehaviorPrototype

    public class CellGridBehaviorPrototype : Prototype
    {
        public ulong BehaviorId { get; protected set; }
    }

    public class BlacklistCellPrototype : Prototype
    {
        public int X { get; protected set; }
        public int Y { get; protected set; }
    }

    public class CellGridBlacklistBehaviorPrototype : CellGridBehaviorPrototype
    {
        public BlacklistCellPrototype[] Blacklist { get; protected set; }
    }

    public class CellGridBorderBehaviorPrototype : CellGridBehaviorPrototype
    {
        public bool DoBorder { get; protected set; }
        public int BorderWidth { get; protected set; }
    }

    public class CellGridRampBehaviorPrototype : CellGridBehaviorPrototype
    {
        public ulong EdgeStart { get; protected set; }
        public ulong EdgeEnd { get; protected set; }
        public float Increment { get; protected set; }
    }

    #endregion

    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X { get; protected set; }
        public sbyte Y { get; protected set; }
        public ulong Cell { get; protected set; }
        public ulong[] Alts { get; protected set; }
    }

    public class SuperCellPrototype : Prototype
    {
        public SuperCellEntryPrototype[] Entries { get; protected set; }
    }
}
