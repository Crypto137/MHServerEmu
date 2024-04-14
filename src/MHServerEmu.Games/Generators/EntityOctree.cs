using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities;
using System.Collections;
using MHServerEmu.Core.Collisions;

namespace MHServerEmu.Games.Generators
{
    [Flags]
    public enum EntityRegionSPContextFlags
    {
        ActivePartition = 1 << 0,
        StaticPartition = 1 << 1,
        PlayersPartition = 1 << 2
    }

    public class EntityRegionSPContext
    {
        public EntityRegionSPContextFlags Flags;
        public ulong PlayerRestrictedGuid;

        public EntityRegionSPContext(EntityRegionSPContextFlags flags = EntityRegionSPContextFlags.ActivePartition | EntityRegionSPContextFlags.StaticPartition)
        {
            Flags = flags;
        }
    }

    public class EntityRegionSpatialPartition
    {
        private WorldEntityRegionSpatialPartition _staticSpatialPartition;
        private WorldEntityRegionSpatialPartition _activeSpatialPartition;
        private HashSet<Avatar> _avatars;
        private Dictionary<ulong, WorldEntityRegionSpatialPartition> _players;
        private Aabb _bounds;
        private float _minRadius;
        public int AvatarIteratorCount { get; protected set; }
        public int TotalElements { get; protected set; }

        public EntityRegionSpatialPartition(Aabb bound, float minRadius = 64.0f)
        {
            _bounds = new(bound);
            _minRadius = minRadius;
            _staticSpatialPartition = new(bound, minRadius, EntityRegionSPContextFlags.StaticPartition);
            _activeSpatialPartition = new(bound, minRadius, EntityRegionSPContextFlags.ActivePartition);
            _players = new();
            _avatars = new();
            AvatarIteratorCount = 0;
            TotalElements = 0;
        }

        public bool Update(WorldEntity element)
        {
            var loc = element.SpatialPartitionLocation;
            if (loc.IsValid() == false)
            {
                return Insert(element);
            }
            else
            {
                var node = loc.Node;
                if (node != null)
                {
                    var tree = node.Tree;
                    if (tree != null)
                        return tree.Update(element);
                }
                return false;
            }
        }

        public bool Remove(WorldEntity element)
        {
            var loc = element.SpatialPartitionLocation;
            if (loc.IsValid() == false) return false;
            TotalElements--;

            if (element is Avatar avatar)
                if (AvatarIteratorCount == 0) _avatars.Remove(avatar);

            var node = loc.Node;
            if (node != null)
            {
                var tree = node.Tree;
                if (tree != null) return tree.Remove(element);
            }
            return false;
        }

        public bool Insert(WorldEntity element)
        {
            bool result;
            ulong restrictedToPlayerGuid = 0; // TODO element.GetProperty<ulong>(PropertyEnum.RestrictedToPlayerGuid);
            if (restrictedToPlayerGuid == 0)
            {
                if (element.IsNeverAffectedByPowers
                    || (element.IsHotspot && element.IsCollidableHotspot == false && element.IsReflectingHotspot == false))
                    result = _staticSpatialPartition.Insert(element);
                else
                    result = _activeSpatialPartition.Insert(element);

                if (element is Avatar avatar)
                {
                    // Debug.Assert(_avatarIteratorCount == 0);
                    if (_avatars.Contains(avatar) == false)
                        _avatars.Add(avatar);
                }
            }
            else
            {
                var spatialPartition = _players.GetValueOrDefault(restrictedToPlayerGuid);
                if (spatialPartition == null)
                {
                    spatialPartition = new(_bounds, _minRadius, EntityRegionSPContextFlags.PlayersPartition);
                    _players[restrictedToPlayerGuid] = spatialPartition;
                }
                result = spatialPartition.Insert(element);
            }
            TotalElements++;
            return result;
        }

        public static bool DoesSphereContainAvatar(Sphere sphere, Avatar avatar)
        {
            if (avatar != null && sphere.Intersects(avatar.RegionLocation.GetPosition())) return true;
            return false;
        }

        public class ElementIterator<B> : IEnumerator<WorldEntity> where B : IBounds
        {
            private List<WorldEntityRegionSpatialPartition> _partitions;
            public WorldEntityRegionSpatialPartition.ElementIterator<B> Iterator { get; private set; }
            public WorldEntity Current => Iterator.Current;
            object IEnumerator.Current => Current;

            public ElementIterator(B bound)
            {
                _partitions = new();
                Iterator = new(bound);
            }

            public void Push(WorldEntityRegionSpatialPartition partition)
            {
                if (Iterator.Tree == null)
                    Iterator.Initialize(partition);
                else if (Iterator.End())
                {
                    var volume = Iterator.Volume;
                    Iterator.Clear();
                    Iterator = new(partition, volume);
                }
                else
                    _partitions.Add(partition);
            }

            public bool MoveNext()
            {
                Iterator.MoveNext();
                while(Iterator.End() && _partitions.Count > 0)
                {
                    var partition = _partitions.LastOrDefault();
                    _partitions.Remove(partition);
                    if (partition == null) return true;
                    var iterator = new WorldEntityRegionSpatialPartition.ElementIterator<B>(partition, Iterator.Volume);
                    Iterator.Clear();
                    Iterator = iterator;

                }
                return true;
            }

            public void Reserve(int size) => _partitions.Capacity = size;
            public IEnumerator<WorldEntity> GetEnumerator() => this;
            public void Reset() => Iterator.Reset();
            public void Dispose() { }
            public void Clear() => Iterator.Clear();
            public bool End() => Iterator.End();
        }

        public IEnumerable<WorldEntity> IterateElementsInVolume<B>(B bound, EntityRegionSPContext context) where B : IBounds
        {
            var iterator = new ElementIterator<B>(bound);
            if (context.Flags.HasFlag(EntityRegionSPContextFlags.ActivePartition))
                iterator.Iterator.Initialize(_activeSpatialPartition);

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.PlayersPartition))
            {
                iterator.Reserve(_players.Count + (context.Flags.HasFlag(EntityRegionSPContextFlags.StaticPartition) ? 1 : 0));

                foreach (var pair in _players)
                    iterator.Push(pair.Value);
            }
            else if (context.PlayerRestrictedGuid != 0)
            {
                if (_players.TryGetValue(context.PlayerRestrictedGuid, out var partition))
                {
                    iterator.Reserve(1 + (context.Flags.HasFlag(EntityRegionSPContextFlags.StaticPartition) ? 1 : 0));
                    iterator.Push(partition);
                }
            }

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.StaticPartition))
                iterator.Push(_staticSpatialPartition);

            try
            {
                while (iterator.End() == false)
                {
                    var element = iterator.Current;
                    iterator.MoveNext();
                    yield return element;
                }
            }
            finally
            {
                iterator.Clear();
            }
        }
    }

    public class EntityRegionSpatialPartitionLocation : QuadtreeLocation<WorldEntity>
    {
        public EntityRegionSpatialPartitionLocation(WorldEntity element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    public class WorldEntityRegionSpatialPartition : Quadtree<WorldEntity>
    {
        public WorldEntityRegionSpatialPartition(Aabb bound, float minRadius, EntityRegionSPContextFlags flag) : base(bound, minRadius) 
        {
            Flag = flag;
        }

        public override QuadtreeLocation<WorldEntity> GetLocation(WorldEntity element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(WorldEntity element) => element.RegionBounds;

        public EntityRegionSPContextFlags Flag { get; private set; }

    }
}
