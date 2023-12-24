namespace MHServerEmu.Games.GameData.Prototypes
{
    public class TowerAreaGeneratorPrototype : GeneratorPrototype
    {
        public int CellSize { get; private set; }
        public int CellSpacing { get; private set; }
        public TowerAreaEntryPrototype[] Entries { get; private set; }
    }

    #region TowerAreaEntryPrototype

    public class TowerAreaEntryPrototype : Prototype
    {
    }

    public class TowerAreaRandomSeqCellsEntryPrototype : TowerAreaEntryPrototype
    {
        public int CellMax { get; private set; }
        public int CellMin { get; private set; }
        public CellSetEntryPrototype[] CellSets { get; private set; }
    }

    public class TowerAreaStaticCellEntryPrototype : TowerAreaEntryPrototype
    {
        public ulong Cell { get; private set; }
        public ulong Name { get; private set; }
    }

    #endregion
}
