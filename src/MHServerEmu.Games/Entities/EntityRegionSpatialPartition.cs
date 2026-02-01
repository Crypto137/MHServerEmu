using System.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.SpatialPartitions;

namespace MHServerEmu.Games.Entities
{
    // NOTE: This file contains the EntityRegionSpatialPartition class, which in the original game appears to had been located in
    // D:\mirrorBuilds_source05\MarvelGame_v52\Source\Game\Game\Entity\EntityOctree.h / .cpp

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
        private readonly Dictionary<ulong, WorldEntityRegionSpatialPartition> _playerRestrictedPartitions = new();
        private readonly List<Avatar> _avatars = new();

        private readonly Aabb _bounds;
        private readonly float _minRadius;

        private int _avatarIteratorCount = 0;

        public int TotalElements { get; private set; } = 0;

        public EntityRegionSpatialPartition(in Aabb bound, float minRadius = 64.0f)
        {
            _primaryPartition = new(bound, minRadius, EntityRegionSPContextFlags.PrimaryPartition);
            _notAffectedByPowersPartition = new(bound, minRadius, EntityRegionSPContextFlags.NotAffectedByPowersPartition);

            // AABB and radius are saved to create player-restricted partitions for each player on demand.
            _bounds = bound;
            _minRadius = minRadius;
        }

        public bool Update(WorldEntity element)
        {
            var loc = element.SpatialPartitionLocation;
            if (loc.IsValid == false)
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
            if (loc.IsValid == false) return false;
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
            ulong restrictedToPlayerGuid = element.Properties[PropertyEnum.RestrictedToPlayerGuid];

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

        public ElementIterator<TVolume> IterateElementsInVolume<TVolume>(TVolume volume, EntityRegionSPContext context) where TVolume : IBounds
        {
            ElementIterator<TVolume> iterator = new(volume);

            // NOTE: We do not need to call reserve like the client does because we are using pooled lists here that only need to be resized once.
            // We also do not initialize the subiterator for the primary partition here directly, because we handle this inside Push().

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.PrimaryPartition))
                iterator.Push(_primaryPartition);

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.PlayerRestrictedPartitions))
            {
                foreach (WorldEntityRegionSpatialPartition partition in _playerRestrictedPartitions.Values)
                    iterator.Push(partition);
            }
            else if (context.PlayerRestrictedGuid != 0)
            {
                if (_playerRestrictedPartitions.TryGetValue(context.PlayerRestrictedGuid, out WorldEntityRegionSpatialPartition partition))
                    iterator.Push(partition);
            }

            if (context.Flags.HasFlag(EntityRegionSPContextFlags.NotAffectedByPowersPartition))
                iterator.Push(_notAffectedByPowersPartition);

            return iterator;
        }

        public struct ElementIterator<TVolume> : IDisposable where TVolume: IBounds
        {
            private readonly TVolume _volume;

            private WorldEntityRegionSpatialPartition _initialPartition;
            private List<WorldEntityRegionSpatialPartition> _partitions;

            public ElementIterator(TVolume volume)
            {
                _partitions = ListPool<WorldEntityRegionSpatialPartition>.Instance.Get();
                _volume = volume;
            }

            public void Dispose()
            {
                // Ownership of the partition list should be transferred to the Enumerator instance,
                // see the Enumerator constructor below for more information.
                if (_partitions != null)
                {
                    Logger.Warn("Dispose(): _partitions != null");
                    ListPool<WorldEntityRegionSpatialPartition>.Instance.Return(_partitions);
                    _partitions = null;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new(ref this);
            }

            public void Push(WorldEntityRegionSpatialPartition partition)
            {
                // This should not be called after we create the Enumerator for this ElementIterator.
                if (_partitions == null)
                    throw new InvalidOperationException();

                if (_initialPartition == null)
                    _initialPartition = partition;
                else
                    _partitions.Add(partition);
            }

            public struct Enumerator : IEnumerator<WorldEntity>
            {
                private readonly WorldEntityRegionSpatialPartition _initialPartition;
                private readonly List<WorldEntityRegionSpatialPartition> _partitions;
                private readonly TVolume _volume;

                private bool _isInitialized;
                private WorldEntityRegionSpatialPartition.ElementIterator<TVolume>.Enumerator _subIterator;

                private bool _isDisposed;

                public WorldEntity Current { get; private set; }
                object IEnumerator.Current { get => Current; }

                public Enumerator(ref ElementIterator<TVolume> iterator)
                {
                    // Right now we use the partition list from our base ElementIterator directly
                    // because we need to return it to the pool after we finish iterating.
                    // To avoid going back and adding using / Dispose() to existing code everywhere,
                    // we do this in the Dispose() implementation of the Enumerator, which is called by foreach.
                    // This makes each iterator "one-off", but for our use case this is okay.

                    _initialPartition = iterator._initialPartition;
                    _partitions = iterator._partitions;
                    _volume = iterator._volume;

                    iterator._initialPartition = null;
                    iterator._partitions = null;
                }

                public bool MoveNext()
                {
                    if (_initialPartition == null || _partitions == null)
                        return false;

                    // Initialize the first partition subiterator.
                    if (_isInitialized == false)
                    {
                        _subIterator = _initialPartition.IterateElementsInVolume(_volume).GetEnumerator();
                        _isInitialized = true;
                    }

                    // Move to the next element in the current subiterator.
                    if (_subIterator.MoveNext())
                    {
                        Current = _subIterator.Current;
                        return true;
                    }

                    // Move over to the next partition if we are finished with the current one.
                    while (_partitions.Count > 0)
                    {
                        int index = _partitions.Count - 1;
                        WorldEntityRegionSpatialPartition partition = _partitions[index];
                        _partitions.RemoveAt(index);

                        if (partition == null)
                            return Logger.WarnReturn(false, "MoveNext(): partition == null");

                        _subIterator.Dispose();
                        _subIterator = partition.IterateElementsInVolume(_volume).GetEnumerator();

                        // Return if this partition has valid elements.
                        if (_subIterator.MoveNext())
                        {
                            Current = _subIterator.Current;
                            return true;
                        }
                    }

                    // We are out of elements and partitions.
                    Current = null;
                    return false;
                }

                public void Reset()
                {
                }

                public void Dispose()
                {
                    if (_isDisposed)
                        return;

                    if (_isInitialized)
                        _subIterator.Dispose();

                    if (_partitions != null)
                        ListPool<WorldEntityRegionSpatialPartition>.Instance.Return(_partitions);

                    _isDisposed = true;
                }
            }
        }


        public RegionAvatarIterator IterateAvatarsInVolume(Sphere volume)
        {
            return new(this, volume);
        }

        public void GetElementsInVolume<TVolume>(List<WorldEntity> elements, TVolume volume, EntityRegionSPContext context) where TVolume : IBounds
        {
            foreach (WorldEntity element in IterateElementsInVolume(volume, context))
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

    public sealed class EntityRegionSpatialPartitionLocation : QuadtreeLocation<WorldEntity>
    {
        public override Aabb Bounds { get => Element.RegionBounds; }

        public EntityRegionSpatialPartitionLocation(WorldEntity element) : base(element) { }
    }

    public sealed class WorldEntityRegionSpatialPartition : Quadtree<WorldEntity>
    {
        public EntityRegionSPContextFlags Flag { get; private set; }

        public WorldEntityRegionSpatialPartition(in Aabb bound, float minRadius, EntityRegionSPContextFlags flag) : base(bound, minRadius)
        {
            Flag = flag;
        }

        public override string ToString()
        {
            return Enum.GetName(Flag);
        }

        public override QuadtreeLocation<WorldEntity> GetLocation(WorldEntity element)
        {
            return element.SpatialPartitionLocation;
        }

        public override Aabb GetElementBounds(WorldEntity element)
        {
            return element.RegionBounds;
        }
    }
}
