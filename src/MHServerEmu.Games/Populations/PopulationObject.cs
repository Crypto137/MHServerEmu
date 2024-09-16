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
        public SpawnLocation SpawnLocation;
        public bool Critical;
        public TimeSpan Time;
        public bool IsMarker;
        public SpawnEvent SpawnEvent;
        public SpawnScheduler Scheduler;
        public ulong SpawnGroupId;
        public bool RemoveOnSpawnFail;

        public bool SpawnByMarker()
        {
            SpawnTarget spawnTarget = new(SpawnLocation.Region)
            {
                Type = SpawnTargetType.Marker
            };
            return SpawnObject(spawnTarget, new());
        }

        public bool SpawnInCell(Cell cell)
        {
            SpawnTarget spawnTarget = new(SpawnLocation.Region)
            {
                Type = SpawnTargetType.RegionBounds,
                RegionBounds = cell.RegionBounds
            };
            return SpawnObject(spawnTarget, new());
        }

        public bool SpawnObject(SpawnTarget spawnTarget, List<WorldEntity> entities)
        {
            ClusterGroup clusterGroup = new(spawnTarget.Region, Random, Object, null, Properties, SpawnFlags);
            clusterGroup.Initialize();

            if (spawnTarget.Type == SpawnTargetType.Marker)
            {
                Region region = spawnTarget.Region;
                SpawnMarkerRegistry registry = region.SpawnMarkerRegistry;
                SpawnReservation reservation = registry.ReserveFreeReservation(MarkerRef, Random, SpawnLocation, clusterGroup.SpawnFlags);
                if (reservation != null)
                {
                    reservation.Object = Object;
                    reservation.MissionRef = MissionRef;
                    spawnTarget.Reservation = reservation;
                }
                else return false;
            }

            bool success = spawnTarget.PlaceClusterGroup(clusterGroup);
            if (success)
            {
                SpawnGroupId = clusterGroup.Spawn(null, Spawner, entities);
                success = SpawnGroupId != SpawnGroup.InvalidId;
            }

            if (success == false && spawnTarget.Reservation != null) 
                spawnTarget.Reservation.State = MarkerState.Free;

            if (success && SpawnEvent != null)
                SpawnEvent.SetSpawnData(SpawnGroupId, entities);

            return success;
        }

        public int GetPriority()
        {
            if (Time > TimeSpan.Zero)
                return (int)Time.TotalMilliseconds;
            return SpawnLocation.SpawnAreas.Count;
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
                    clusterGroup.Reservation = Reservation;
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
