namespace MHServerEmu.Games.GameData.Prototypes
{
    public class TowerAreaGeneratorPrototype : GeneratorPrototype
    {
        public int CellSize { get; set; }
        public int CellSpacing { get; set; }
        public TowerAreaEntryPrototype[] Entries { get; set; }
    }

    #region TowerAreaEntryPrototype

    public class TowerAreaEntryPrototype : Prototype
    {
    }

    public class TowerAreaRandomSeqCellsEntryPrototype : TowerAreaEntryPrototype
    {
        public int CellMax { get; set; }
        public int CellMin { get; set; }
        public CellSetEntryPrototype[] CellSets { get; set; }
    }

    public class TowerAreaStaticCellEntryPrototype : TowerAreaEntryPrototype
    {
        public ulong Cell { get; set; }
        public ulong Name { get; set; }
    }

    #endregion
}
