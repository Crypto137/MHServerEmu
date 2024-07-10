using System.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common.SpatialPartitions;
using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.Games.Entities
{
    [Flags]
    public enum EntityRegionSPContextFlags
    {
        ActivePartition = 1 << 0,
        StaticPartition = 1 << 1,
        PlayersPartition = 1 << 2,
        All = ActivePartition | StaticPartition | PlayersPartition
    }

    public readonly struct EntityRegionSPContext
    {
        public readonly EntityRegionSPContextFlags Flags;
        public readonly ulong PlayerRestrictedGuid;

        public EntityRegionSPContext()
        {
            Flags = EntityRegionSPContextFlags.ActivePartition | EntityRegionSPContextFlags.StaticPartition;
            PlayerRestrictedGuid = 0;
        }

        public EntityRegionSPContext(EntityRegionSPContextFlags flags, ulong playerRestrictedGuid = 0)
        {
            Flags = flags;
            PlayerRestrictedGuid = playerRestrictedGuid;
        }
    }

    public class EntityRegionSpatialPartition
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private WorldEntityRegionSpatialPartition _staticSpatialPartition;
        private WorldEntityRegionSpatialPartition _activeSpatialPartition;
        private List<Avatar> _avatars;
        private Dictionary<ulong, WorldEntityRegionSpatialPartition> _players;
        private Aabb _bounds;
        private float _minRadius;
        public int AvatarIteratorCount { get; protected set; }
        public int TotalElements { get; protected set; }

        public EntityRegionSpatialPartition(in Aabb bound, float minRadius = 64.0f)
        {
            _bounds = bound;
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
                    if (tree == null) return Logger.WarnReturn(false, "Update(): tree == null");
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
                    || element.IsHotspot && element.IsCollidableHotspot == false && element.IsReflectingHotspot == false)
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

        public static bool DoesSphereContainAvatar(in Sphere sphere, Avatar avatar)
        {
            if (avatar != null && sphere.Intersects(avatar.RegionLocation.Position)) return true;
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
                while (Iterator.End() && _partitions.Count > 0)
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

        public IEnumerable<Avatar> IterateAvatarsInVolume(Sphere volume)
        {
            var iterator = new RegionAvatarIterator(this, volume);

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

        public void GetElementsInVolume<B>(List<WorldEntity> elements, B volume, EntityRegionSPContext context) where B : IBounds
        {
            foreach (var element in IterateElementsInVolume(volume, context))
                elements.Add(element);
        }

        public class RegionAvatarIterator : IEnumerator<Avatar>
        {
            private EntityRegionSpatialPartition _spatialPartition;
            private Sphere _volume;
            private int _current;

            public RegionAvatarIterator(EntityRegionSpatialPartition spatialPartition, in Sphere volume)
            {
                _spatialPartition = spatialPartition;
                _volume = volume;
                _current = 0;

                IncrementIteratorCount();
                if (_spatialPartition != null)
                {
                    for (int index = 0; index < _spatialPartition._avatars.Count; index++)
                    {
                        Avatar entity = _spatialPartition._avatars[index];
                        if (entity != null && DoesSphereContainAvatar(_volume, entity))
                        {
                            _current = index;
                            return;
                        }
                    }
                    _current = int.MaxValue;
                }
            }

            public bool MoveNext()
            {
                if (_spatialPartition != null && _current < _spatialPartition._avatars.Count)
                {
                    _current++;
                    for (; _current < _spatialPartition._avatars.Count; _current++)
                    {
                        Avatar entity = _spatialPartition._avatars[_current];
                        if (entity != null && DoesSphereContainAvatar(_volume, entity))
                            break;
                    }
                }
                return true;
            }

            public Avatar Current => End() ? null : _spatialPartition._avatars[_current];
            object IEnumerator.Current => Current;
            public void Dispose() { }
            public void Reset() { }
            public bool End() => _spatialPartition == null || _current >= _spatialPartition._avatars.Count;
            public void Clear() => DecrementIteratorCount();

            private void IncrementIteratorCount()
            {
                if (_spatialPartition != null) _spatialPartition.AvatarIteratorCount++;
            }

            private void DecrementIteratorCount()
            {
                if (_spatialPartition != null) _spatialPartition.AvatarIteratorCount--;
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
        public WorldEntityRegionSpatialPartition(in Aabb bound, float minRadius, EntityRegionSPContextFlags flag) : base(bound, minRadius)
        {
            Flag = flag;
        }

        public override QuadtreeLocation<WorldEntity> GetLocation(WorldEntity element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(WorldEntity element) => element.RegionBounds;

        public EntityRegionSPContextFlags Flag { get; private set; }

    }
}
