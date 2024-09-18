using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Populations
{
    public class PopulationObject
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public PrototypeId MarkerRef;
        public PrototypeId MissionRef;
        public GRandom Random;
        public PropertyCollection Properties;
        public SpawnFlags SpawnFlags;
        public PopulationObjectPrototype Object;
        public WorldEntity Spawner;
        public List<PrototypeId> SpawnAreas;
        public List<PrototypeId> SpawnCells;
        public int Count;

        public bool SpawnByMarker(Cell cell)
        {
            if (Count == 0) return false;
            SpawnTarget spawnTarget = new(cell.Region)
            {
                Type = SpawnTargetType.Marker,
                Cell = cell
            };
            if (SpawnObject(spawnTarget, out _) != 0)
            {
                Count--;
                return true;
            }
            return false;
        }

        public bool SpawnInCell(Cell cell)
        {
            SpawnTarget spawnTarget = new(cell.Region)
            {
                Type = SpawnTargetType.RegionBounds,
                RegionBounds = cell.RegionBounds
            };

            while (Count > 0)
            {
                SpawnObject(spawnTarget, out _);
                Count--;
            }
            if (Count == 0) return true;
            return false;
        }

        public ulong SpawnObject(SpawnTarget spawnTarget, out List<WorldEntity> entities)
        {
            ulong groupId = 0;
            entities = new();

            if (spawnTarget.Type == SpawnTargetType.Marker)
            {
                Region region = spawnTarget.Region;
                SpawnMarkerRegistry registry = region.SpawnMarkerRegistry;
                SpawnReservation reservation = registry.ReserveFreeReservation(MarkerRef, Random, spawnTarget.Cell, SpawnAreas, SpawnCells);
                if (reservation != null)
                {
                    reservation.Object = Object;
                    reservation.MissionRef = MissionRef;
                    spawnTarget.Reservation = reservation;
                }
                else return groupId;
            }

            ClusterGroup clusterGroup = new(spawnTarget.Region, Random, Object, null, Properties, SpawnFlags);
            clusterGroup.Initialize();
            bool success = spawnTarget.PlaceClusterGroup(clusterGroup);
            if (success) groupId = clusterGroup.Spawn(null, Spawner, entities);
            return groupId;
        }
    }
    public enum SpawnTargetType
    {
        Marker,
        Spawner,
        RegionBounds
    }

    public class SpawnTarget
    {
        public SpawnTargetType Type;
        public PrototypeId Marker;
        public RegionLocation Location;
        public SpawnerPrototype SpawnerProto;
        public Region Region;
        public Aabb RegionBounds;
        public Cell Cell;
        public SpawnReservation Reservation;

        public SpawnTarget(Region region)
        {
            Region = region;
        }

        public bool PlaceClusterGroup(ClusterGroup clusterGroup)
        {
            bool success = false;
            switch (Type)
            {
                case SpawnTargetType.Marker:
                    clusterGroup.SetParentRelativePosition(Reservation.GetRegionPosition());
                    clusterGroup.SetParentRelativeOrientation(Reservation.MarkerRot); // can be random?                    
                    clusterGroup.TestLayout();
                    success = true;
                    break;

                case SpawnTargetType.RegionBounds:
                    success = clusterGroup.PickPositionInBounds(RegionBounds);
                    break;

                case SpawnTargetType.Spawner:
                    Vector3 pos = Location.Position;
                    Orientation rot = Location.Orientation;

                    success = clusterGroup.PickPositionInSector(pos, rot, SpawnerProto.SpawnDistanceMin, SpawnerProto.SpawnDistanceMax);
                    if (success == false && SpawnerProto.SpawnFailBehavior.HasFlag(SpawnFailBehavior.RetryForce))
                    {
                        clusterGroup.SetParentRelativePosition(pos);
                        success = true;
                    }
                    break;
            }

            return success;
        }
    }
}
