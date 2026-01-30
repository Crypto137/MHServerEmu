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
        PrimaryPartition                = 1 << 0,   // Most things go here.
        NotAffectedByPowersPartition    = 1 << 1,   // Entities not affected by powers and hotspots that are not collidable / reflecting.
        PlayerRestrictedPartitions      = 1 << 2,   // Player-specific entities (e.g. instanced loot).

        UnrestrictedPartitions          = PrimaryPartition | NotAffectedByPowersPartition,
        AllPartitions                   = PrimaryPartition | NotAffectedByPowersPartition | PlayerRestrictedPartitions
    }

    public readonly struct EntityRegionSPContext
    {
        public readonly EntityRegionSPContextFlags Flags;
        public readonly ulong PlayerRestrictedGuid;

        public EntityRegionSPContext()
        {
            Flags = EntityRegionSPContextFlags.AllPartitions;
            PlayerRestrictedGuid = 0;
        }

        public EntityRegionSPContext(EntityRegionSPContextFlags flags, ulong playerRestrictedGuid = 0)
        {
            Flags = flags;
            PlayerRestrictedGuid = playerRestrictedGuid;
        }

        public EntityRegionSPContext(ulong playerRestrictedGuid)
        {
            Flags = EntityRegionSPContextFlags.UnrestrictedPartitions;
            PlayerRestrictedGuid = playerRestrictedGuid;
        }
    }

    public class EntityRegionSpatialPartition
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WorldEntityRegionSpatialPartition _primaryPartition;
        private readonly WorldEntityRegionSpatialPartition _notAffectedByPowersPartition;
        private Dictionary<ulong, WorldEntityRegionSpatialPartition> _playerRestrictedPartitions = new();
        private List<Avatar> _avatars = new();

        private Aabb _bounds;
        private float _minRadius;

        private int _avatarIteratorCount = 0;

        public int TotalElements { get; private set; } = 0;

        public EntityRegionSpatialPartition(in Aabb bound, float minRadius = 64.0f)
        {
            _primaryPartition = new(bound, minRadius, EntityRegionSPContextFlags.PrimaryPartition);
            _notAffectedByPowersPartition = new(bound, minRadius, EntityRegionSPContextFlags.NotAffectedByPowersPartition);
            _bounds = bound;
            _minRadius = minRadius;
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
            {
                if (_avatarIteratorCount != 0)
                    Logger.Warn("Remove(): _avatarIteratorCount != 0");

                _avatars.Remove(avatar);
            }

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
                if (element.IsNeverAffectedByPowers ||
                    (element.IsHotspot && element.IsCollidableHotspot == false && element.IsReflectingHotspot == false))
                    result = _notAffectedByPowersPartition.Insert(element);
                else
                    result = _primaryPartition.Insert(element);

                if (element is Avatar avatar)
                {
                    if (_avatarIteratorCount != 0)
                        Logger.Warn("Insert(): _avatarIteratorCount != 0");

                    if (_avatars.Contains(avatar) == false)
                        _avatars.Add(avatar);
                }
            }
            else
            {
                if (_playerRestrictedPartitions.TryGetValue(restrictedToPlayerGuid, out var spatialPartition) == false)
                {
                    spatialPartition = new(_bounds, _minRadius, EntityRegionSPContextFlags.PlayerRestrictedPartitions);
                    _playerRestrictedPartitions.Add(restrictedToPlayerGuid, spatialPartition);
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
            if (context.Flags.HasFlag(EntityRegionSPContextFlags.PrimaryPartition))
                iterator.Iterator.Initialize(_primaryPartition);

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.PlayerRestrictedPartitions))
            {
                iterator.Reserve(_playerRestrictedPartitions.Count + (context.Flags.HasFlag(EntityRegionSPContextFlags.NotAffectedByPowersPartition) ? 1 : 0));

                foreach (var pair in _playerRestrictedPartitions)
                    iterator.Push(pair.Value);
            }
            else if (context.PlayerRestrictedGuid != 0)
            {
                if (_playerRestrictedPartitions.TryGetValue(context.PlayerRestrictedGuid, out var partition))
                {
                    iterator.Reserve(1 + (context.Flags.HasFlag(EntityRegionSPContextFlags.NotAffectedByPowersPartition) ? 1 : 0));
                    iterator.Push(partition);
                }
            }

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.NotAffectedByPowersPartition))
                iterator.Push(_notAffectedByPowersPartition);

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

        public RegionAvatarIterator IterateAvatarsInVolume(Sphere volume)
        {
            return new(this, volume);
        }

        public void GetElementsInVolume<B>(List<WorldEntity> elements, B volume, EntityRegionSPContext context) where B : IBounds
        {
            foreach (var element in IterateElementsInVolume(volume, context))
                elements.Add(element);
        }

        public readonly struct RegionAvatarIterator
        {
            private readonly EntityRegionSpatialPartition _spatialPartition;
            private readonly Sphere _volume;

            public RegionAvatarIterator(EntityRegionSpatialPartition spatialPartition, in Sphere volume)
            {
                _spatialPartition = spatialPartition;
                _volume = volume;
            }

            public Enumerator GetEnumerator()
            {
                return new(this);
            }

            public struct Enumerator : IEnumerator<Avatar>
            {
                private readonly EntityRegionSpatialPartition _spatialPartition;
                private readonly Sphere _volume;

                private int _current;

                public Avatar Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(RegionAvatarIterator iterator)
                {
                    _spatialPartition = iterator._spatialPartition;
                    _volume = iterator._volume;

                    if (_spatialPartition != null)
                        _spatialPartition._avatarIteratorCount++;

                    _current = -1;
                }

                public bool MoveNext()
                {
                    if (_spatialPartition == null)
                        return false;

                    while (++_current < _spatialPartition._avatars.Count)
                    {
                        Avatar avatar = _spatialPartition._avatars[_current];

                        if (DoesSphereContainAvatar(_volume, avatar) == false)
                            continue;

                        Current = avatar;
                        return true;
                    }

                    Current = null;
                    return false;
                }

                public void Reset()
                {
                    _current = -1;
                }

                public void Dispose()
                {
                    if (_spatialPartition != null)
                        _spatialPartition._avatarIteratorCount--;
                }
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
