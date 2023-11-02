using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class CanyonGridAreaGeneratorPrototype : GeneratorPrototype
    {
        public CanyonCellChoiceListPrototype Cells;
        public short Length;
        public RegionDirection ConnectOnBridgeOnlyDirection;

        public CanyonGridAreaGeneratorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CanyonGridAreaGeneratorPrototype), proto); }
    }

    public class CanyonCellChoiceListPrototype : Prototype
    {
        public CellChoicePrototype[] BridgeChoices;
        public CellChoicePrototype[] NormalChoices;
        public CellChoicePrototype[] LeftOrBottomChoices;
        public CellChoicePrototype[] RightOrTopChoices;
        public AreaOrientation Orientation;
        public CanyonCellChoiceListPrototype(Prototype proto) { FillPrototype(typeof(CanyonCellChoiceListPrototype), proto); }
    }

    public enum AreaOrientation
    {
        Horizontal,
        Vertical,
    }

    public class CellChoicePrototype : Prototype
    {
        public ulong Cell;
        public int Weight;
        public CellChoicePrototype(Prototype proto) { FillPrototype(typeof(CellChoicePrototype), proto); }
    }
}
