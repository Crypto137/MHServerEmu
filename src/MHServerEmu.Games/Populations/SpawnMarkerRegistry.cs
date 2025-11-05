using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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
        private Picker<SpawnReservation> _reusablePicker = new();   // TODO: Replace with pooling
        public TimeSpan _respawnDelay;

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
                if (_region.Aabb.FullyContains(managedObject.RegionBounds) == false)
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
            CellPrototype cellProto = cell.Prototype;
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
                        if (markerRef == PrototypeId.Invalid) continue;

                        Vector3 cellPos = entityMarker.Position - cell.Prototype.BoundingBox.Center;
                        Vector3 regionPos = cell.RegionBounds.Center + cellPos;

                        if (cell.RegionBounds.IntersectsXY(regionPos) == false)
                            Logger.Warn($"[DESIGN]Trying to add marker outside of cell bounds. " +
                                $"CELL={GameDatabase.GetFormattedPrototypeName(cell.PrototypeDataRef)}, BOUNDS={cell.RegionBounds}, " +
                                $"MARKER={GameDatabase.GetFormattedPrototypeName(markerRef)}, REGIONPOS={regionPos}, CELLPOS={marker.Position}");

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
            var reservations = ListPool<SpawnReservation>.Instance.Get();
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
            ListPool<SpawnReservation>.Instance.Return(reservations);
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

        bool PickReservation(Picker<SpawnReservation> picker, PrototypeId markerRef, SpawnLocation spawnLocation, SpawnFlags flag)
        {
            var spawnAreas = spawnLocation.SpawnAreas;
            var spawnCells = spawnLocation.SpawnCells;

            if (spawnCells.Count > 0)
            {
                foreach (var spawnCell in spawnCells)
                {
                    var spawnCellId = spawnCell.Id;
                    if (_cellLookup.TryGetValue(spawnCellId, out var spawnMap) == false || spawnMap == null) continue;
                    if (spawnMap.TryGetValue(markerRef, out var list) == false || list == null) continue;
                    var spawnArea = spawnCell.Area;
                    foreach (var testReservation in list)
                    {
                        if (spawnAreas.Count > 0 && spawnAreas.Contains(spawnArea) == false) continue;
                        if (TestReservation(testReservation, flag, true))
                            picker.Add(testReservation);
                    }
                }
            }
            else if (spawnAreas.Count > 0)
            {
                foreach (var spawnArea in spawnAreas)
                {
                    var spawnAreaRef = spawnArea.PrototypeDataRef;
                    if (_areaLookup.TryGetValue(spawnAreaRef, out var spawnMap) == false || spawnMap == null) continue;
                    if (spawnMap.TryGetValue(markerRef, out var list) == false || list == null) continue;
                    foreach (var testReservation in list)
                        if (TestReservation(testReservation, flag, true))
                            picker.Add(testReservation);
                }
            }
            else
            {
                if (_regionLookup.TryGetValue(markerRef, out var list) && list != null)
                    foreach (var testReservation in list)
                        if (TestReservation(testReservation, flag, true))
                            picker.Add(testReservation);
            }
            return picker.Empty() == false;
        }

        public SpawnReservation ReserveFreeReservation(PrototypeId markerRef, GRandom random, SpawnLocation spawnLocation, SpawnFlags flag, int respawnDelayMS)
        {
            Picker<SpawnReservation> picker = _reusablePicker;  // TODO: replace with pooling
            picker.Initialize(random);
            _respawnDelay = TimeSpan.FromMilliseconds(respawnDelayMS);

            bool canPick = PickReservation(picker, markerRef, spawnLocation, flag);

            if (canPick == false && flag.HasFlag(SpawnFlags.IgnoreSimulated))
            {
                flag &= ~SpawnFlags.IgnoreSimulated;
                canPick = PickReservation(picker, markerRef, spawnLocation, flag);
            }

            if (canPick == false && flag.HasFlag(SpawnFlags.IgnoreBlackout) == false)
            {
                flag |= SpawnFlags.IgnoreBlackout;
                canPick = PickReservation(picker, markerRef, spawnLocation, flag);
            }

            if (canPick && picker.Pick(out SpawnReservation reservation))
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
            var reservations = ListPool<SpawnReservation>.Instance.Get();
            try
            {
                GetReservationsInCell((uint)cellId, reservations);
                foreach (var reservation in reservations)
                    if (reservation.Id == markerId) return reservation;

                return null;
            }
            finally
            {
                ListPool<SpawnReservation>.Instance.Return(reservations);
            }
        }

        public SpawnReservation GetReservationInCell(uint cellId, int id)
        {
            var reservations = ListPool<SpawnReservation>.Instance.Get();
            try
            {
                GetReservationsInCell(cellId, reservations);
                foreach (var reservation in reservations)
                    if (reservation.Id == id) return reservation;
                return null;
            }
            finally
            {
                ListPool<SpawnReservation>.Instance.Return(reservations);
            }
        }

        public void GetPositionsByMarker(PrototypeId markerRef, List<Vector3> positions)
        {
            if (_regionLookup.TryGetValue(markerRef, out var list) && list != null)
                foreach (var testReservation in list)
                    positions.Add(testReservation.GetRegionPosition());
        }

        public void OnSimulation(Cell cell, int numPlayers)
        {
            var reservations = ListPool<SpawnReservation>.Instance.Get();

            if (numPlayers == 0)
            {
                GetReservationsInCell(cell.Id, reservations);
                foreach (var reservation in reservations)
                    if (reservation.Cell == cell)
                    {
                        reservation.Simulated = false;
                        reservation.LastFreeTime = TimeSpan.Zero;
                    }
            }
            else if (numPlayers == 1)
            {
                GetReservationsInCell(cell.Id, reservations);
                foreach (var reservation in reservations)
                    if (reservation.Cell == cell)
                        reservation.Simulated = true;
            }

            ListPool<SpawnReservation>.Instance.Return(reservations);
        }

        public bool TestReservation(SpawnReservation reservation, SpawnFlags flag, bool checkRespawn = false, bool checkFree = true)
        {
            if (checkFree && reservation.State != MarkerState.Free) return false;
            if (flag.HasFlag(SpawnFlags.IgnoreSimulated) && reservation.Simulated) return false;
            if (flag.HasFlag(SpawnFlags.IgnoreBlackout) == false && reservation.BlackOutZones > 0) return false;
            if (checkRespawn && _respawnDelay != TimeSpan.Zero)
            {
                var reservationTime = Game.Current.CurrentTime - reservation.LastFreeTime;
                if (reservationTime < _respawnDelay) return false;
            }
            return true;
        }

        public int CalcFreeReservation(PrototypeId markerRef, SpawnFlags flag = SpawnFlags.IgnoreBlackout)
        {
            int count = 0;
            if (_regionLookup.TryGetValue(markerRef, out var list) && list != null)
                foreach (var testReservation in list)
                    if (TestReservation(testReservation, flag)) count++;
            return count;
        }

        public int CalcMarkerReservations(PrototypeId markerRef, PrototypeId spawnAreaRef, SpawnFlags flag = SpawnFlags.IgnoreBlackout)
        {
            int count = 0;
            if (_areaLookup.TryGetValue(spawnAreaRef, out var spawnMap) && spawnMap != null)
                if (spawnMap.TryGetValue(markerRef, out var list) == false && list != null)
                    foreach (var testReservation in list)
                        if (TestReservation(testReservation, flag, false, false)) count++;
            return count;
        }

        public void AddBlackOutZone(BlackOutZone zone)
        {
            foreach (var reservation in _reservationOctree.IterateElementsInVolume(zone.Sphere))
                reservation.BlackOutZones++;
        }

        public void RemoveBlackOutZone(BlackOutZone zone)
        {
            foreach (var reservation in _reservationOctree.IterateElementsInVolume(zone.Sphere))
                reservation.BlackOutZones--;
        }
    }
}
