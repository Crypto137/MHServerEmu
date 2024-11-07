using Gazillion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
    public class ReservedSpawn
    {
        public AssetId Asset { get; }
        public int Id { get; }
        public bool UseMarkerOrientation { get; }

        public ReservedSpawn(AssetId asset, int id, bool useMarkerOrientation)
        {
            Asset = asset;
            Id = id;
            UseMarkerOrientation = useMarkerOrientation;
        }

        public NetStructReservedSpawn ToNetStruct()
        {
            return NetStructReservedSpawn.CreateBuilder()
                .SetAsset((ulong)Asset)
                .SetId((uint)Id)
                .SetUseMarkerOrientation(UseMarkerOrientation)
                .Build();
        }
    }
}
