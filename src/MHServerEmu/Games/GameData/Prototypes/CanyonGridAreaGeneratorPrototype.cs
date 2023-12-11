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
        public CanyonCellChoiceListPrototype Cells { get; set; }
        public short Length { get; set; }
        public RegionDirection ConnectOnBridgeOnlyDirection { get; set; }
    }

    public class CanyonCellChoiceListPrototype : Prototype
    {
        public CellChoicePrototype[] BridgeChoices { get; set; }
        public CellChoicePrototype[] NormalChoices { get; set; }
        public CellChoicePrototype[] LeftOrBottomChoices { get; set; }
        public CellChoicePrototype[] RightOrTopChoices { get; set; }
        public AreaOrientation Orientation { get; set; }
    }

    public class CellChoicePrototype : Prototype
    {
        public ulong Cell { get; set; }
        public int Weight { get; set; }
    }
}
