using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class ReservedSpawn
    {
        public AssetId Asset { get; }
        public uint Id { get; }
        public bool UseMarkerOrientation { get; }

        public ReservedSpawn(AssetId asset, uint id, bool useMarkerOrientation)
        {
            Asset = asset;
            Id = id;
            UseMarkerOrientation = useMarkerOrientation;
        }

        public NetStructReservedSpawn ToNetStruct() => 
            NetStructReservedSpawn.CreateBuilder()
            .SetAsset((ulong)Asset)
            .SetId(Id)
            .SetUseMarkerOrientation(UseMarkerOrientation)
            .Build();
    }
}
