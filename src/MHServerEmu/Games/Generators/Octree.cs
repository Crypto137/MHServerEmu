using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Regions;
using System.Collections;

namespace MHServerEmu.Games.Generators
{
    public class EntityRegionSpatialPartition
    {
        private WorldEntityRegionSpatialPartition _quadtree1;
        private WorldEntityRegionSpatialPartition _quadtree2;

        public EntityRegionSpatialPartition(Aabb bound, float v2 = 64.0f )
        {
            _quadtree1 = new(bound, v2);
            _quadtree2 = new(bound, v2);
        }
    }

    public class SpatialPartitionLocation // QuadtreeLocation<Cell,CellRegionSpatialPartitionElementOps<Cell>,24>
    {
        public object Node { get; private set; }
        public SpatialPartitionLocation() { Node = null; }

        public bool IsValid()
        {
            return Node != null;
        }
    }

    // TODO: Implement Quadtree class

    public class Quadtree<T>
    {
        private readonly List<T> _simpleList = new();

        public Quadtree(Aabb bound, int v1, float v2, float v3)
        {

        }

        public object Insert(T item)
        {
            _simpleList.Add(item); // Simple way
            return item;
        }

        public virtual bool Intersects(T item, Aabb volume)
        {
            return true;
        }

        public IEnumerable<T> IterateElementsInVolume(Aabb volume)
        {
            return _simpleList.Where(item => Intersects(item, volume));  // Simple way
        }

        public object Remove(T item)
        {
            _simpleList.Remove(item); // Simple way
            return item;
        }

        public class ElementIterator : IEnumerable<T>
        {
            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class SpawnReservationSpatialPartition : Quadtree<SpawnReservation>  // Quadtree<SpawnReservation,SpawnReservationSpatialPartitionElementOps<SpawnReservation>,24>
    {
        public SpawnReservationSpatialPartition(Aabb bound): base (bound, 6, 128.0f, 2.0f) { }

        public override bool Intersects(SpawnReservation item, Aabb volume)
        {
            return volume.Intersects(item.RegionBounds);
        }
    }

    public class CellSpatialPartition : Quadtree<Cell>  // Quadtree<Cell,CellRegionSpatialPartitionElementOps<Cell>,24>
    {
        public CellSpatialPartition(Aabb bound) : base(bound, 6, 128.0f, 2.0f) { }

        public override bool Intersects(Cell item, Aabb volume)
        {
            return volume.Intersects(item.RegionBounds);
        }

    }

    public class WorldEntityRegionSpatialPartition : Quadtree<WorldEntity>  // Quadtree<WorldEntity,EntityRegionSpatialPartitionElementOps<WorldEntity>,24>
    {
        public WorldEntityRegionSpatialPartition(Aabb bound, float v2) : base(bound, 6, v2, 2.0f) { }

        public override bool Intersects(WorldEntity entity, Aabb volume)
        {
            throw new NotImplementedException();
        }

    }
}
