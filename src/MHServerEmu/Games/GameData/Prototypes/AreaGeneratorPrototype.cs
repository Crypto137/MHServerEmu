using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GeneratorPrototype : Prototype
    {
    }

    public class DistrictAreaGeneratorPrototype : GeneratorPrototype
    {
        public AssetId District { get; protected set; }
    }

    public class AreaGenerationInterfacePrototype : GeneratorPrototype
    {
    }

    public class SingleCellAreaGeneratorPrototype : GeneratorPrototype
    {
        public AssetId Cell { get; protected set; }
        public int BorderWidth { get; protected set; }
        public CellSetEntryPrototype[] BorderCellSets { get; protected set; }
    }

    public class CellSetEntryPrototype : Prototype
    {
        public AssetId CellSet { get; protected set; }
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
        public PrototypeId Area { get; protected set; }
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
        public AssetId BehaviorId { get; protected set; }
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
        public AssetId EdgeStart { get; protected set; }
        public AssetId EdgeEnd { get; protected set; }
        public float Increment { get; protected set; }
    }

    #endregion

    public class SuperCellEntryPrototype : Prototype
    {
        public sbyte X { get; protected set; }
        public sbyte Y { get; protected set; }
        public AssetId Cell { get; protected set; }
        public AssetId[] Alts { get; protected set; }
    }

    public class SuperCellPrototype : Prototype
    {
        public SuperCellEntryPrototype[] Entries { get; protected set; }
    }
}
