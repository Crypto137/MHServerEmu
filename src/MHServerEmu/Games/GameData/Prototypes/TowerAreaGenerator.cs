namespace MHServerEmu.Games.GameData.Prototypes
{
    public class TowerAreaGeneratorPrototype : GeneratorPrototype
    {
        public int CellSize { get; protected set; }
        public int CellSpacing { get; protected set; }
        public TowerAreaEntryPrototype[] Entries { get; protected set; }
    }

    #region TowerAreaEntryPrototype

    public class TowerAreaEntryPrototype : Prototype
    {
    }

    public class TowerAreaRandomSeqCellsEntryPrototype : TowerAreaEntryPrototype
    {
        public int CellMax { get; protected set; }
        public int CellMin { get; protected set; }
        public CellSetEntryPrototype[] CellSets { get; protected set; }
    }

    public class TowerAreaStaticCellEntryPrototype : TowerAreaEntryPrototype
    {
        public ulong Cell { get; protected set; }
        public ulong Name { get; protected set; }
    }

    #endregion
}
