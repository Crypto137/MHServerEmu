using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class GeneratorPrototype : Prototype
    {
        public GeneratorPrototype(Prototype proto) { FillPrototype(typeof(GeneratorPrototype), proto); }
    }

    public class DistrictAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong District;
        public DistrictAreaGeneratorPrototype(Prototype proto) : base(proto) 
        { 
            FillPrototype(typeof(DistrictAreaGeneratorPrototype), proto); 
        }
    }

    public class AreaGenerationInterfacePrototype : GeneratorPrototype
    {
        public AreaGenerationInterfacePrototype(Prototype proto) : base(proto) 
        { 
            FillPrototype(typeof(AreaGenerationInterfacePrototype), proto); 
        }
    }

    public class SingleCellAreaGeneratorPrototype : GeneratorPrototype
    {
        public ulong Cell;
        public int BorderWidth;
        public CellSetEntryPrototype[] BorderCellSets;

        public SingleCellAreaGeneratorPrototype(Prototype proto) : base(proto)
        {
            FillPrototype(typeof(SingleCellAreaGeneratorPrototype), proto);
        }
    }

    public class CellSetEntryPrototype : Prototype
    {
        public ulong CellSet;
        public int Weight;
        public bool Unique;
        public IgnoreOfTypeEntryPrototype IgnoreOfType;

        public CellSetEntryPrototype(Prototype proto) { FillPrototype(typeof(CellSetEntryPrototype), proto); }
    }

    public class IgnoreOfTypeEntryPrototype : Prototype
    {
        public int Ignore;

        public IgnoreOfTypeEntryPrototype(Prototype proto) { FillPrototype(typeof(IgnoreOfTypeEntryPrototype), proto); }
    }

    public class RequiredPOIAreaEntryPrototype : Prototype
    {
        public ulong Area;
        public int Picks;

        public RequiredPOIAreaEntryPrototype(Prototype proto) { FillPrototype(typeof(RequiredPOIAreaEntryPrototype), proto); }
    }

    public class RequiredPOIGroupPrototype : Prototype
    {
        public RequiredPOIAreaEntryPrototype[] Areas;
        public RequiredCellBasePrototype[] RequiredCells;

        public RequiredPOIGroupPrototype(Prototype proto) { FillPrototype(typeof(RequiredPOIGroupPrototype), proto); }
    }

    #region CellGridBehaviorPrototype

    public class CellGridBehaviorPrototype : Prototype
    {
        public ulong BehaviorId;

        public CellGridBehaviorPrototype(Prototype proto) { FillPrototype(typeof(CellGridBehaviorPrototype), proto); }
    }
    public class BlacklistCellPrototype : Prototype
    {
        public int X;
        public int Y;

        public BlacklistCellPrototype(Prototype proto) { FillPrototype(typeof(BlacklistCellPrototype), proto); }
    }

    public class CellGridBlacklistBehaviorPrototype : CellGridBehaviorPrototype
    {
        public BlacklistCellPrototype[] Blacklist;

        public CellGridBlacklistBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CellGridBlacklistBehaviorPrototype), proto); }
    }

    public class CellGridBorderBehaviorPrototype : CellGridBehaviorPrototype
    {
        public bool DoBorder;
        public int BorderWidth;

        public CellGridBorderBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CellGridBorderBehaviorPrototype), proto); }
    }

    public class CellGridRampBehaviorPrototype : CellGridBehaviorPrototype
    {
        public ulong EdgeStart;
        public ulong EdgeEnd;
        public float Increment;

        public CellGridRampBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CellGridRampBehaviorPrototype), proto); }
    };

    #endregion
}
