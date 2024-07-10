using System.Text;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common.SpatialPartitions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
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
        public MarkerType Type { get; private set; }
        public int Id { get; private set; }
        public Cell Cell { get; private set; }
        public MarkerState State { get; set; }
        public Vector3 MarkerPos { get; private set; }
        public Orientation MarkerRot { get; private set; }
        public PrototypeId MarkerRef { get; private set; }
        public Sphere RegionSphere { get; private set; }
        public Aabb RegionBounds { get; private set; }

        public SpawnReservationSpatialPartitionLocation SpatialPartitionLocation { get; }
        public PopulationObjectPrototype Object { get; set; }
        public PrototypeId MissionRef { get; set; }

        public SpawnReservation(SpawnMarkerRegistry registry, PrototypeId markerRef, MarkerType type, Vector3 position, Orientation rotation, Cell cell, int id)
        {
            _registry = registry;
            MarkerRef = markerRef;
            Type = type;
            State = MarkerState.Free;
            MarkerPos = position;
            MarkerRot = rotation;
            Cell = cell;
            Id = id;
            SpatialPartitionLocation = new(this);
            CalculateRegionInfo();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            if (MissionRef != PrototypeId.Invalid)
                sb.AppendLine($"MissionRef: {GameDatabase.GetFormattedPrototypeName(MissionRef)}");
            sb.AppendLine($"Position: {MarkerPos.ToString()}");
            sb.AppendLine($"CellId: {Cell.Id}");
            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"State: {State}");
            if (Object != null)
                sb.AppendLine($"Object: {Object}");
            return sb.ToString();
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

        public int GetPid()
        {
            return (int)Cell.Id * 1000 + Id;
        }
    }

    public class SpawnReservationSpatialPartitionLocation : QuadtreeLocation<SpawnReservation>
    {
        public SpawnReservationSpatialPartitionLocation(SpawnReservation element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    public class SpawnReservationSpatialPartition : Quadtree<SpawnReservation>
    {
        public SpawnReservationSpatialPartition(in Aabb bound) : base(bound, 128.0f) { }

        public override QuadtreeLocation<SpawnReservation> GetLocation(SpawnReservation element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(SpawnReservation element) => element.RegionBounds;
    }
}
