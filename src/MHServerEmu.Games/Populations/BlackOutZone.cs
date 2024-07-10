using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common.SpatialPartitions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Populations
{
    public class BlackOutZone
    {
        public ulong Id { get; private set; }
        public Sphere Sphere { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public PrototypeId MissionRef { get; private set; }

        public BlackOutSpatialPartitionLocation SpatialPartitionLocation { get; internal set; }

        public BlackOutZone(ulong id, Vector3 position, float radius, PrototypeId missionRef)
        {
            Id = id;
            Sphere = new Sphere(position, radius);
            RegionBounds = Sphere.ToAabb();
            MissionRef = missionRef;
            SpatialPartitionLocation = new(this);
        }
    }

    public class BlackOutSpatialPartitionLocation : QuadtreeLocation<BlackOutZone>
    {
        public BlackOutSpatialPartitionLocation(BlackOutZone element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    public class BlackOutSpatialPartition : Quadtree<BlackOutZone>
    {
        public BlackOutSpatialPartition(in Aabb bound) : base(bound, 128.0f) { }

        public override QuadtreeLocation<BlackOutZone> GetLocation(BlackOutZone element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(BlackOutZone element) => element.RegionBounds;
    }
}
