using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
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
                if (cell == null) continue;
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

        public IEnumerable<SpawnReservation> IterateReservationsInVolume<B>(B bound) where B : IBounds
        {
            if (_reservationOctree != null)
                return _reservationOctree.IterateElementsInVolume(bound);
            else
                return Enumerable.Empty<SpawnReservation>();
        }

        public void InitializeSpacialPartition(in Aabb bound)
        {
            if (_reservationOctree != null) return;
            _reservationOctree = new(bound);

            foreach (SpawnReservation reservation in _spawnReservations)
            {
                if (reservation == null) continue;
                reservation.CalculateRegionInfo();

                SpawnReservation managedObject = reservation;
                if (_region.Bound.FullyContains(managedObject.RegionBounds) == false)
                {
                    Logger.Trace("Trying to insert Marker out of bounds in Spatial Partition! " +
                                      $"MARKER={GameDatabase.GetFormattedPrototypeName(managedObject.MarkerRef)} " +
                                      $"Area={managedObject.Cell.Area} CELL={managedObject.Cell} MARKERPOS={managedObject.MarkerPos}");
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

                    if (cell.Region.CheckMarkerFilter(filterRef))
                    {
                        if (entityMarker.EntityGuid == 0) continue;
                        var markerRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                        if (markerRef == 0) continue;

                        Vector3 cellPos = entityMarker.Position - cell.CellProto.BoundingBox.Center;
                        Vector3 regionPos = cell.RegionBounds.Center + cellPos;

                        if (!cell.RegionBounds.IntersectsXY(regionPos))
                        {
                            Logger.Warn($"[DESIGN]Trying to add marker outside of cell bounds. " +
                                $"CELL={GameDatabase.GetFormattedPrototypeName(cell.PrototypeId)}, BOUNDS={cell.RegionBounds}, " +
                                $"MARKER={GameDatabase.GetFormattedPrototypeName(markerRef)}, REGIONPOS={regionPos}, CELLPOS={marker.Position}");
                            continue;
                        }
                        //Logger.Debug($"Marker [{GameDatabase.GetFormattedPrototypeName(markerRef)}] {regionPos}");
                        AddSpawnTypeLocation(markerRef, marker.Position, marker.Rotation, cell, ++id);
                    }
                }
            }
        }

        private void AddSpawnTypeLocation(PrototypeId markerRef, Vector3 position, Orientation rotation, Cell cell, int id)
        {
            if (markerRef == 0) return;
            SpawnMarkerPrototype spawnMarkerProto = GameDatabase.GetPrototype<SpawnMarkerPrototype>(markerRef);
            if (spawnMarkerProto == null) return;

            MarkerType type = spawnMarkerProto.Type;
            SpawnReservation spot = new(this, markerRef, type, position, rotation, cell, id);
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
                regionList = new();
                _regionLookup[markerRef] = regionList;
            }
            regionList.Add(spot);

            PrototypeId areaRef = cell.Area.PrototypeDataRef;
            if (!_areaLookup.TryGetValue(areaRef, out var areaMap))
            {
                areaMap = new();
                _areaLookup[areaRef] = areaMap;
            }

            if (!areaMap.TryGetValue(markerRef, out var areaList))
            {
                areaList = new();
                areaMap[markerRef] = areaList;
            }
            areaList.Add(spot);

            uint cellId = cell.Id;
            if (!_cellLookup.TryGetValue(cellId, out var cellMap))
            {
                cellMap = new();
                _cellLookup[cellId] = cellMap;
            }

            if (!cellMap.TryGetValue(markerRef, out var cellList))
            {
                cellList = new();
                cellMap[markerRef] = cellList;
            }
            cellList.Add(spot);
        }

        public void RemoveCell(Cell cell)
        {
            List<SpawnReservation> reservations = new();
            GetReservationsInCell(cell.Id, reservations);

            foreach (SpawnReservation reservation in reservations)
            {
                if (reservation == null || reservation.Cell != cell) continue;
                if (_reservationOctree != null && reservation.SpatialPartitionLocation.IsValid()) _reservationOctree.Remove(reservation);
                bool success = true;
                success &= RemoveFromMasterVector(reservation);
                success &= RemoveFromRegionLookup(reservation);
                success &= RemoveFromAreaLookup(reservation);
                success &= RemoveFromCellLookup(reservation);
                if (success == false) Logger.Warn($"RemoveCell failed {cell}");
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
            if (reservation == null || reservation.Cell == null) return false;

            var area = reservation.Cell.Area;
            if (area == null) return false;

            var areaRef = area.PrototypeDataRef;
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

        public SpawnReservation ReserveFreeReservation(PrototypeId markerRef, GRandom random, Cell spawnCell, List<PrototypeId> spawnAreas, List<PrototypeId> spawnCells)
        {
            Picker<SpawnReservation> picker = new(random);

            var spawnCellRef = spawnCell.PrototypeId;
            var spawnCellId = spawnCell.Id;
            var spawnAreaRef = spawnCell.Area.PrototypeDataRef;

            // picker add
            if (spawnCells.Any())
            {
                foreach (var cellref in spawnCells)
                {
                    if (spawnCellRef != cellref) continue;
                    if (_cellLookup.TryGetValue(spawnCellId, out var spawnMap) == false || spawnMap == null) continue;
                    if (spawnMap.TryGetValue(markerRef, out var list) == false || list == null) continue;
                    foreach (var testReservation in list)
                    {
                        if (testReservation.State != MarkerState.Free) continue;
                        if (spawnAreas?.Contains(spawnAreaRef) == false) continue;
                        picker.Add(testReservation);
                    }
                }
            }
            else if (spawnAreas.Any())
            {
                foreach (var areaRef in spawnAreas)
                {
                    if (areaRef != spawnAreaRef) continue;
                    if (_areaLookup.TryGetValue(spawnAreaRef, out var spawnMap) == false || spawnMap == null) continue;
                    if (spawnMap.TryGetValue(markerRef, out var list) == false || list == null) continue;
                    foreach (var testReservation in list)
                    {
                        if (testReservation.State != MarkerState.Free) continue;
                        picker.Add(testReservation);
                    }
                }
            }

            if (picker.Empty() == false && picker.Pick(out SpawnReservation reservation))
            {
                reservation.State = MarkerState.Reserved;
                return reservation;
            }
            return null;
        }

        public SpawnReservation GetReservationByPid(int pid)
        {
            int cellId = pid / 1000;
            int markerId = pid % 1000;
            List<SpawnReservation> reservations = new();
            GetReservationsInCell((uint)cellId, reservations);
            foreach (var reservation in reservations)
                if (reservation.Id == markerId) return reservation;

            return null;
        }

        public int CalcFreeReservation(PrototypeId markerRef, PrototypeId spawnAreaRef)
        {
            int count = 0;
            if (_areaLookup.TryGetValue(spawnAreaRef, out var spawnMap) && spawnMap != null)
                if (spawnMap.TryGetValue(markerRef, out var list) == false && list != null)
                    foreach (var testReservation in list)
                        if (testReservation.State == MarkerState.Free) count++;
            return count;
        }
    }
}
