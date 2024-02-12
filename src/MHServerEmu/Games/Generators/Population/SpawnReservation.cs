using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnReservationMap : Dictionary<PrototypeId, SpawnReservationList> { };
    public class SpawnReservationList : List<SpawnReservation> { };

    public enum MarkerState
    {
        Free,
        Reserved
    }

    public class SpawnReservation
    {
        private SpawnMarkerRegistry _registry;
        private MarkerType _type;        
        private int _id;
        public Cell Cell { get; private set; }
        public MarkerState State { get; set; }
        public Vector3 MarkerPos { get; private set; }
        public Vector3 MarkerRot { get; private set; }
        public PrototypeId MarkerRef { get; private set; }
        public Sphere RegionSphere { get; private set; }
        public Aabb RegionBounds { get; private set; }

        public SpawnReservationSpatialPartitionLocation SpatialPartitionLocation { get; }

        public SpawnReservation(SpawnMarkerRegistry registry, PrototypeId markerRef, MarkerType type, Vector3 position, Vector3 rotation, Cell cell, int id)
        {
            _registry = registry;
            MarkerRef = markerRef;
            _type = type;
            State = MarkerState.Free;
            MarkerPos = position;
            MarkerRot = rotation;
            Cell = cell;
            _id = id;
            SpatialPartitionLocation = new(this);
            CalculateRegionInfo();
        }

        public Vector3 GetRegionPosition()
        {
            return RegionBounds.Center;
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
