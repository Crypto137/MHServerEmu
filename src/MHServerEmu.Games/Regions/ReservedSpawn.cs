using Gazillion;

namespace MHServerEmu.Games.Regions
{
    public class ReservedSpawn
    {
        public ulong Asset { get; }
        public uint Id { get; }
        public bool UseMarkerOrientation { get; }

        public ReservedSpawn(ulong asset, uint id, bool useMarkerOrientation)
        {
            Asset = asset;
            Id = id;
            UseMarkerOrientation = useMarkerOrientation;
        }

        public NetStructReservedSpawn ToNetStruct() => NetStructReservedSpawn.CreateBuilder().SetAsset(Asset).SetId(Id).SetUseMarkerOrientation(UseMarkerOrientation).Build();
    }
}
