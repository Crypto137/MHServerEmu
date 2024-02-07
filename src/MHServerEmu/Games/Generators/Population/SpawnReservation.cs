using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnReservationMap : Dictionary<PrototypeId, SpawnReservationList> { };
    public class SpawnReservationList : List<SpawnReservation> { };

    public class SpawnReservation
    {
        private SpawnMarkerRegistry registry;
        private MarkerType type;
        private int id;
        public Cell Cell { get; private set; }
        public Vector3 MarkerPos { get; private set; }
        public Vector3 MarkerRot { get; private set; }
        public PrototypeId MarkerRef { get; private set; }
        public Sphere RegionSphere { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public SpawnReservationSpatialPartitionLocation SpatialPartitionLocation { get; }

        public SpawnReservation(SpawnMarkerRegistry registry, PrototypeId markerRef, MarkerType type, Vector3 position, Vector3 rotation, Cell cell, int id)
        {
            this.registry = registry;
            MarkerRef = markerRef;
            this.type = type;
            MarkerPos = position;
            MarkerRot = rotation;
            Cell = cell;
            this.id = id;
            SpatialPartitionLocation = new(this);
            CalculateRegionInfo();
        }

        public void CalculateRegionInfo()
        {
            Vector3 cellLocalPos = MarkerPos - Cell.CellProto.BoundingBox.Center;
            Vector3 regionPos = Cell.RegionBounds.Center + cellLocalPos;
            RegionSphere = new Sphere(regionPos, 64.0f);
            RegionBounds = RegionSphere.ToAabb();
        }

    }
}
