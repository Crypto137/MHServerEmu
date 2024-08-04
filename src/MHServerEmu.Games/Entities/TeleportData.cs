using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Entities
{
    public struct TeleportData
    {
        public ulong RegionId { get; private set; }
        public Vector3 Position { get; private set; }
        public Orientation Orientation { get; private set; }

        public bool IsValid { get => RegionId != 0; }

        public void Set(ulong regionId, in Vector3 position, in Orientation orientation)
        {
            RegionId = regionId;
            Position = position;
            Orientation = orientation;
        }

        public void Clear()
        {
            RegionId = 0;
            Position = Vector3.Zero;
            Orientation = Orientation.Zero;
        }
    }
}
