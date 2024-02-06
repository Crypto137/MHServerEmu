using MHServerEmu.Games.GameData.Calligraphy.Attributes;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Horizontal)]
    public enum AreaOrientation
    {
        Horizontal,
        Vertical,
    }

    #endregion

    public class CanyonGridAreaGeneratorPrototype : GeneratorPrototype
    {
        public CanyonCellChoiceListPrototype Cells { get; protected set; }
        public short Length { get; protected set; }
        public RegionDirection ConnectOnBridgeOnlyDirection { get; protected set; }
    }

    public class CanyonCellChoiceListPrototype : Prototype
    {
        public CellChoicePrototype[] BridgeChoices { get; protected set; }
        public CellChoicePrototype[] NormalChoices { get; protected set; }
        public CellChoicePrototype[] LeftOrBottomChoices { get; protected set; }
        public CellChoicePrototype[] RightOrTopChoices { get; protected set; }
        public AreaOrientation Orientation { get; protected set; }
    }

    public class CellChoicePrototype : Prototype
    {
        public AssetId Cell { get; protected set; }
        public int Weight { get; protected set; }
    }
}
