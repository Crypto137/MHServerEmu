using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class AreaSettings
    {
        public uint Id { get; set; }
        public Vector3 Origin { get; set; }
        public RegionSettings RegionSettings { get; set; }
        public PrototypeId AreaDataRef { get; set; }
        public bool IsStartArea { get; set; }
    }
}
