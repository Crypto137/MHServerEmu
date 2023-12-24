using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum AreaOrientation
    {
        Horizontal,
        Vertical,
    }

    #endregion

    public class CanyonGridAreaGeneratorPrototype : GeneratorPrototype
    {
        public CanyonCellChoiceListPrototype Cells { get; private set; }
        public short Length { get; private set; }
        public RegionDirection ConnectOnBridgeOnlyDirection { get; private set; }
    }

    public class CanyonCellChoiceListPrototype : Prototype
    {
        public CellChoicePrototype[] BridgeChoices { get; private set; }
        public CellChoicePrototype[] NormalChoices { get; private set; }
        public CellChoicePrototype[] LeftOrBottomChoices { get; private set; }
        public CellChoicePrototype[] RightOrTopChoices { get; private set; }
        public AreaOrientation Orientation { get; private set; }
    }

    public class CellChoicePrototype : Prototype
    {
        public ulong Cell { get; private set; }
        public int Weight { get; private set; }
    }
}
