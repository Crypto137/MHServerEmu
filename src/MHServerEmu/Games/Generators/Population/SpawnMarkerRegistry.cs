using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnMarkerRegistry
    {
        private Region _region;
        private SpawnReservationSpatialPartition _reservationOctree;
        private List<SpawnReservation> _spawnReservations = new();

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
            throw new NotImplementedException();
        }

        internal void RemoveCell(Cell cell)
        {
            throw new NotImplementedException();
        }
    }

    public class SpawnReservation
    {
        public Cell Cell { get; internal set; }
        public Vector3 MarkerPos { get; internal set; }
        public ulong MarkerRef { get; internal set; }
        public Aabb RegionBounds { get; internal set; }

        internal void CalculateRegionInfo()
        {
            throw new NotImplementedException();
        }
    }
}
