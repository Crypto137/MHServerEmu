using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;

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
        public Vector3? Position;
        public SpawnHeat SpawnHeat;
        public bool IsMarker;
        public bool IsMissionMarker;
        public ulong SpawnGroupId;
        public bool SpawnCleanup;
        public bool RemoveOnSpawnFail;
        public SpawnEvent SpawnEvent;
        public SpawnScheduler Scheduler;
        public SpawnReservation MarkerReservation;

        public void ResetMarker()
        {
            MarkerReservation?.ResetReservation(false);
        }

        public bool SpawnByMarker(List<WorldEntity> entities)
        {
            SpawnTarget spawnTarget = new(SpawnLocation.Region)
            {
                Type = SpawnTargetType.Marker
            };
            return SpawnObject(spawnTarget, entities);
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

        public bool SpawnInPosition(Vector3 position)
        {
            SpawnTarget spawnTarget = new(SpawnLocation.Region)
            {
                Type = SpawnTargetType.Position,
                Position = position,
            };
            return SpawnObject(spawnTarget, new());
        }

        public void CleanUpSpawnFlags()
        {
            if (SpawnCleanup) SpawnFlags |= SpawnFlags.Cleanup;
        }

        public bool SpawnObject(SpawnTarget spawnTarget, List<WorldEntity> entities)
        {
            CleanUpSpawnFlags();
            ClusterGroup clusterGroup = new(spawnTarget.Region, Random, Object, null, Properties, SpawnFlags);
            clusterGroup.Initialize();

            if (spawnTarget.Type == SpawnTargetType.Marker)
            {
                Region region = spawnTarget.Region;
                SpawnMarkerRegistry registry = region.SpawnMarkerRegistry;

                SpawnReservation reservation = MarkerReservation;
                if (reservation != null)
                    reservation.State = MarkerState.Reserved;
                else
                {
                    int respawnDelayMS = SpawnEvent != null ? SpawnEvent.RespawnDelayMS : 0;
                    reservation = registry.ReserveFreeReservation(MarkerRef, Random, SpawnLocation, clusterGroup.SpawnFlags, respawnDelayMS);
                }

                if (reservation != null)
                {
                    reservation.Object = Object;
                    reservation.MissionRef = MissionRef;
                    spawnTarget.Reservation = reservation;
                }
                else return false;
            }

            bool success = spawnTarget.TryPlaceClusterGroup(clusterGroup);

            if (success)
            {
                SpawnGroupId = clusterGroup.Spawn(null, Spawner, SpawnHeat, entities);
                success = SpawnGroupId != SpawnGroup.InvalidId;
            }

            if (success == false && spawnTarget.Reservation != null) 
                spawnTarget.Reservation.State = MarkerState.Free;

            if (success && SpawnEvent != null)
                SpawnEvent.SetSpawnData(SpawnGroupId, entities);

            if (PopulationManager.DebugMarker(MarkerRef)) Logger.Warn($"SpawnObject {MarkerRef.GetNameFormatted()}");

            return success;
        }

        public int GetPriority()
        {
            if (Time > TimeSpan.Zero)
                return (int)Time.TotalMilliseconds;
            return SpawnLocation.SpawnAreas.Count;
        }

        public static void GetContainedEncounters(PopulationObjectInstancePrototype[] objectList, List<PopulationObjectInstancePrototype> encounters)
        {
            if (objectList.IsNullOrEmpty()) return;
            foreach (var objectInstance in objectList)
            {
                if (objectInstance == null || objectInstance.Weight <= 0) continue;
                var proto = GameDatabase.GetPrototype<Prototype>(objectInstance.Object);
                if (proto is PopulationObjectPrototype)
                    encounters.Add(objectInstance);
                else if (proto is PopulationObjectListPrototype populationObjectList)
                    GetContainedEncounters(populationObjectList.List, encounters);
            }
        }

        public static Picker<PopulationObjectPrototype> PopulatePicker(GRandom random, PopulationObjectInstancePrototype[] objectList)
        {
            Picker<PopulationObjectPrototype> picker = new(random);
            foreach (var objectInstance in objectList)
            {
                var objectProto = GameDatabase.GetPrototype<PopulationObjectPrototype>(objectInstance.Object);
                if (objectProto == null) continue;
                int weight = objectInstance.Weight;
                if (weight > 0)
                {
                    // LiveTuning PopulationObjectWeight
                    weight = (int)(weight * LiveTuningManager.GetLivePopObjTuningVar(objectProto, PopObjTuningVar.ePOTV_PopulationObjectWeight));

                    picker.Add(objectProto, weight);
                }
            }

            return picker;
        }

        public static bool PickEnemies(GRandom random, int enemyPicks, PopulationObjectInstancePrototype[] objectList, List<PopulationObjectInstancePrototype> enemies)
        {
            if (objectList.IsNullOrEmpty()) return false;

            Picker<PopulationObjectInstancePrototype> picker = new(random);
            foreach (var objectInstance in objectList)
            {
                if (objectInstance == null || objectInstance.Weight <= 0) continue;
                var objectProto = GameDatabase.GetPrototype<PopulationObjectPrototype>(objectInstance.Object);
                if (objectProto == null) continue;
                int weight = objectInstance.Weight;
                if (weight > 0)
                    picker.Add(objectInstance, weight);
            }

            while (picker.Pick(out var objectInstance) && enemyPicks > 0)
            {
                var proto = GameDatabase.GetPrototype<Prototype>(objectInstance.Object);
                if (proto is PopulationObjectPrototype)
                {
                    enemies.Add(objectInstance);
                    enemyPicks--;
                }
                else if (proto is PopulationObjectListPrototype populationObjectList)
                    return PickEnemies(random, enemyPicks, populationObjectList.List, enemies);
            }
            return enemyPicks == 0;
        }

        public override string ToString()
        {
            string type = IsMarker ? "M" : "R";
            if (Position != null) type = "P";
            return $"PopulationObject [{type}] [{MissionRef.GetNameFormatted()}] [{Object}]";
        }
    }

    public enum SpawnTargetType
    {
        Marker,
        Spawner,
        RegionBounds,
        Position
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
        public Vector3 Position;

        public SpawnTarget(Region region)
        {
            Region = region;
        }

        public bool TryPlaceClusterGroup(ClusterGroup clusterGroup)
        {
            bool success = PlaceClusterGroup(clusterGroup);

            if (success == false && clusterGroup.SpawnFlags.HasFlag(SpawnFlags.IgnoreBlackout) == false && clusterGroup.SpawnFlags.HasFlag(SpawnFlags.RetryIgnoringBlackout))
            {
                clusterGroup.SpawnFlags |= SpawnFlags.IgnoreBlackout;
                success = PlaceClusterGroup(clusterGroup);
            }

            if (success == false && Type == SpawnTargetType.Spawner && clusterGroup.SpawnFlags.HasFlag(SpawnFlags.RetryForce))
            {
                clusterGroup.SetParentRelative(Location.Position, Location.Orientation);
                success = true;
            }

            return success;
        }

        public bool PlaceClusterGroup(ClusterGroup clusterGroup)
        {
            bool success = false;
            switch (Type)
            {
                case SpawnTargetType.Marker:
                    clusterGroup.Reservation = Reservation;
                    Vector3 position = Reservation.GetRegionPosition();
                    Orientation orientation = clusterGroup.ObjectProto.UseMarkerOrientation ? Reservation.MarkerRot : Orientation.Player;
                    clusterGroup.SetParentRelative(position, orientation);
                    success = true;
                    break;

                case SpawnTargetType.RegionBounds:
                    success = clusterGroup.PickPositionInBounds(RegionBounds);
                    break;

                case SpawnTargetType.Position:
                    success = clusterGroup.PickPositionInSector(Position, Orientation.Player, 0, SpawnMap.Resolution);
                    break;

                case SpawnTargetType.Spawner:
                    position = Location.Position;
                    orientation = Location.Orientation;
                    success = clusterGroup.PickPositionInSector(position, orientation, SpawnerProto.SpawnDistanceMin, SpawnerProto.SpawnDistanceMax, SpawnerProto.SpawnFacing);
                    break;
            }

            return success;
        }
    }
}
