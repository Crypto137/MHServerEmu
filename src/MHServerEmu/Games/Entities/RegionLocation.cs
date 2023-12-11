using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class RegionLocation
    {
        public Region Region { get; set; }
        public Cell Cell { get; private set; }
        public Area Area { get => Cell.Area; }
        public ulong RegionId { get => Region.Id; }
        public uint AreaId { get => Area.Id; }
        public uint CellId { get => Cell.Id; }
    }
}