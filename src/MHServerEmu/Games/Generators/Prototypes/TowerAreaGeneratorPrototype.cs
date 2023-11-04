using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class TowerAreaGeneratorPrototype : GeneratorPrototype
    {
        public int CellSize;
        public int CellSpacing;
        public new TowerAreaEntryPrototype[] Entries;
        public TowerAreaGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TowerAreaGeneratorPrototype), proto); }
    }

    #region TowerAreaEntryPrototype
    public class TowerAreaEntryPrototype : Prototype
    {
        public TowerAreaEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TowerAreaEntryPrototype), proto); }
    }

    public class TowerAreaRandomSeqCellsEntryPrototype : TowerAreaEntryPrototype
    {
        public int CellMax;
        public int CellMin;
        public CellSetEntryPrototype[] CellSets;
        public TowerAreaRandomSeqCellsEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TowerAreaRandomSeqCellsEntryPrototype), proto); }
    }

    public class TowerAreaStaticCellEntryPrototype : TowerAreaEntryPrototype
    {
        public ulong Cell;
        public ulong Name;
        public TowerAreaStaticCellEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TowerAreaStaticCellEntryPrototype), proto); }
    }
    #endregion
}
