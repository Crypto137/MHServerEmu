using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnMarkerRegistry
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        private Region _region;
        private SpawnReservationSpatialPartition _reservationOctree;
        private SpawnReservationList _spawnReservations = new();
        private SpawnReservationMap _regionLookup = new();
        private Dictionary<ulong, SpawnReservationMap> _areaLookup = new();
        private Dictionary<uint, SpawnReservationMap> _cellLookup = new();

        public SpawnMarkerRegistry(Region region)
        {
            _region = region;
        }

        public bool Initialize()
        {
            Destroy();
            foreach (Cell cell in _region.Cells)
            {
                if (cell == null)  continue;
                AddCell(cell);
            }
            return true;
        }

        private void Destroy()
        {
            throw new NotImplementedException();
        }

        public void InitializeSpacialPartition(Aabb bound)
        {
            if (_reservationOctree != null) return;
            _reservationOctree = new (bound);

            foreach (SpawnReservation reservation in _spawnReservations)
            {
                if (reservation == null) continue;
                reservation.CalculateRegionInfo();

                SpawnReservation managedObject = reservation;
                if (_region.Bound.FullyContains(managedObject.RegionBounds) == false)
                {
                    Console.WriteLine("Trying to insert Marker out of bounds in Spatial Partition! " +
                                      $"MARKER={GameDatabase.GetFormattedPrototypeName(managedObject.MarkerRef)} " +
                                      $"REGION={_region} CELL={managedObject.Cell} MARKERPOS={managedObject.MarkerPos}");
                    continue;
                }

                _reservationOctree.Insert(managedObject);
            }
        }

        public void AddCell(Cell cell)
        {
            int id = 0;
            CellPrototype cellProto = cell.CellProto;
            foreach (var marker in cellProto.MarkerSet.Markers)
            {                
                if (marker is not EntityMarkerPrototype entityMarker) continue;
                SpawnMarkerPrototype spawnMarker = entityMarker.GetMarkedPrototype<SpawnMarkerPrototype>();
                if (spawnMarker != null && spawnMarker.Type != MarkerType.Prop)
                {
                    var filterRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.FilterGuid);

                    if (cell.GetRegion().CheckMarkerFilter(filterRef))
                    {
                        if (entityMarker.EntityGuid == 0) continue;
                        var markerRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                        if (markerRef == 0) continue;

                        Vector3 cellPos = entityMarker.Position - cell.CellProto.BoundingBox.Center;
                        Vector3 regionPos = cell.RegionBounds.Center + cellPos;

                        if (!cell.RegionBounds.IntersectsXY(regionPos))
                        {
                            Logger.Trace($"[DESIGN]Trying to add marker outside of cell bounds. " +
                                $"CELL={GameDatabase.GetFormattedPrototypeName(cell.PrototypeId)}, BOUNDS={cell.RegionBounds}, " +
                                $"MARKER={GameDatabase.GetFormattedPrototypeName(markerRef)}, REGIONPOS={regionPos}, CELLPOS={marker.Position}");
                            continue;
                        }
                        AddSpawnTypeLocation(markerRef, marker.Position, marker.Rotation, cell, ++id);
                    }
                }
            }
        }

        private void AddSpawnTypeLocation(ulong markerRef, Vector3 position, Vector3 rotation, Cell cell, int id)
        {
            if (markerRef == 0)  return;
            SpawnMarkerPrototype spawnMarkerProto = GameDatabase.GetPrototype<SpawnMarkerPrototype>(markerRef);
            if (spawnMarkerProto == null) return;

            MarkerType type = spawnMarkerProto.Type;
            SpawnReservation spot = new (this, markerRef, type, position, rotation, cell, id);
            if (spot == null)  return;

            _spawnReservations.Add(spot);

            if (_reservationOctree != null)
            {
                SpawnReservation managedObject = spot;
                if (managedObject != null && !managedObject.SpatialPartitionLocation.IsValid())
                    _reservationOctree.Insert(managedObject);
            }

            if (!_regionLookup.TryGetValue(markerRef, out var regionList))
            {
                regionList = new ();
                _regionLookup[markerRef] = regionList;
            }
            regionList.Add(spot);

            ulong areaRef = cell.Area.GetPrototypeDataRef();
            if (!_areaLookup.TryGetValue(areaRef, out var areaMap))
            {
                areaMap = new ();
                _areaLookup[areaRef] = areaMap;
            }

            if (!areaMap.TryGetValue(markerRef, out var areaList))
            {
                areaList = new ();
                areaMap[markerRef] = areaList;
            }
            areaList.Add(spot);

            uint cellId = cell.Id;
            if (!_cellLookup.TryGetValue(cellId, out var cellMap))
            {
                cellMap = new ();
                _cellLookup[cellId] = cellMap;
            }

            if (!cellMap.TryGetValue(markerRef, out var cellList))
            {
                cellList = new ();
                cellMap[markerRef] = cellList;
            }
            cellList.Add(spot);
        }

        internal void RemoveCell(Cell cell)
        {
            throw new NotImplementedException();
        }
    }

    public class SpawnReservationMap : Dictionary<ulong, SpawnReservationList> { };
    public class SpawnReservationList : List<SpawnReservation> { };

    public class SpawnReservation
    {
        private SpawnMarkerRegistry registry;
        private MarkerType type;
        private int id;
        public Cell Cell { get; private set; }
        public Vector3 MarkerPos { get; private set; }
        public Vector3 MarkerRot { get; private set; }
        public ulong MarkerRef { get; private set; }
        public Sphere RegionSphere { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public SpawnReservationSpatialPartitionLocation SpatialPartitionLocation { get; }

        public SpawnReservation(SpawnMarkerRegistry registry, ulong markerRef, MarkerType type, Vector3 position, Vector3 rotation, Cell cell, int id)
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
