using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class CellSettings
    {
        public Vector3 PositionInArea { get; set; }
        public Orientation OrientationInArea { get; set; }
        public PrototypeId CellRef { get; set; }
        public int Seed { get; set; }
        public LocaleStringId OverrideLocationName { get; set; }
        public List<uint> ConnectedCells { get; set; }
        public PrototypeId PopulationThemeOverrideRef { get; set; }
    }
}
