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
        private Dictionary<PrototypeId, SpawnReservationMap> _areaLookup = new();
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

        public void Destroy()
        {
            if (_reservationOctree != null)
                foreach (var reservation in _spawnReservations)
                    _reservationOctree.Remove(reservation);

            foreach (var region in _regionLookup)
                region.Value.Clear();

            foreach (var area in _areaLookup)
            {
                foreach (var areaMap in area.Value)
                    areaMap.Value.Clear();
               area.Value.Clear();
            }

            foreach (var cell in _cellLookup)
            {
                foreach (var cellMap in cell.Value)
                    cellMap.Value.Clear();
                cell.Value.Clear();
            }
            _reservationOctree = null;
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
                    Logger.Trace("Trying to insert Marker out of bounds in Spatial Partition! " +
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

        private void AddSpawnTypeLocation(PrototypeId markerRef, Vector3 position, Vector3 rotation, Cell cell, int id)
        {
            if (markerRef == 0) return;
            SpawnMarkerPrototype spawnMarkerProto = GameDatabase.GetPrototype<SpawnMarkerPrototype>(markerRef);
            if (spawnMarkerProto == null) return;

            MarkerType type = spawnMarkerProto.Type;
            SpawnReservation spot = new (this, markerRef, type, position, rotation, cell, id);
            if (spot == null) return;

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

            PrototypeId areaRef = cell.Area.GetPrototypeDataRef();
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

        public void RemoveCell(Cell cell)
        {
            List<SpawnReservation> reservations = new ();
            GetReservationsInCell(cell.Id, reservations);

            foreach (SpawnReservation reservation in reservations)
            {
                reservations.Remove(reservation);
                if (_reservationOctree != null)
                {
                    SpawnReservation managedObject = reservation;
                    if (managedObject != null && managedObject.SpatialPartitionLocation.IsValid())
                        _reservationOctree.Remove(managedObject);
                }
                if ((reservation != null && reservation.Cell == cell) 
                    || RemoveFromMasterVector(reservation)
                    || RemoveFromRegionLookup(reservation)
                    || RemoveFromAreaLookup(reservation)
                    || RemoveFromCellLookup(reservation)
                    // || reservation.use_count() == 1 // std::shared_ptr use_count
                    ) return;
            }
        }

        private void GetReservationsInCell(uint cellId, List<SpawnReservation> reservations)
        {
            if (cellId == 0) return;
            if (_cellLookup.TryGetValue(cellId, out var cellMap) && cellMap != null)
                foreach (var map in cellMap)
                {
                    var list = map.Value;
                    if (list != null)
                    {
                        foreach (var reservation in list)
                            reservations.Add(reservation);
                    }
                }
        }

        private bool RemoveFromMasterVector(SpawnReservation reservation)
        {
            return reservation != null && _spawnReservations.Remove(reservation);
        }

        private bool RemoveFromRegionLookup(SpawnReservation reservation)
        {
            if (reservation == null) return false;

            var markerRef = reservation.MarkerRef;
            if (markerRef == 0) return false;

            if (_regionLookup.TryGetValue(markerRef, out var regionList))
            {
                if (regionList != null && regionList.Remove(reservation))
                {
                    if (regionList.Count == 0) _regionLookup.Remove(markerRef);
                    return true;                  
                }
            }
            return false;
        }

        private bool RemoveFromAreaLookup(SpawnReservation reservation)
        {
            if (reservation == null || reservation.Cell == null)  return false;

            var area = reservation.Cell.Area;
            if (area == null) return false;

            var areaRef = area.GetPrototypeDataRef();
            if (areaRef == 0) return false;

            var markerRef = reservation.MarkerRef;
            if (markerRef == 0) return false;

            if (_areaLookup.TryGetValue(areaRef, out var areaMap))
            {
                if (areaMap != null && areaMap.TryGetValue(markerRef, out var areaList))
                {
                    if (areaList != null && areaList.Remove(reservation))
                        if (areaList.Count == 0) areaMap.Remove(markerRef);                       

                    if (areaMap.Count == 0) _areaLookup.Remove(areaRef);
                    return true;
                }               
            }
            return false;
        }

        private bool RemoveFromCellLookup(SpawnReservation reservation)
        {
            if (reservation == null || reservation.Cell == null) return false;

            var cellId = reservation.Cell.Id;
            if (cellId == 0) return false;

            var markerRef = reservation.MarkerRef;
            if (markerRef == 0) return false;

            if (_cellLookup.TryGetValue(cellId, out var cellMap))
            {
                if (cellMap != null && cellMap.TryGetValue(markerRef, out var cellList))
                {
                    if (cellList != null && cellList.Remove(reservation))
                        if (cellList.Count == 0) cellMap.Remove(markerRef);

                    if (cellMap.Count == 0) _cellLookup.Remove(cellId);
                    return true;
                }                
            }
            return false;
        }

    }

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
