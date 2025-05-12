using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public enum EntityTrackerOptions
    {
        None,
        IsDestroyed
    }

    public class EntityTrackingData
    {
        public Dictionary<ulong, EntityTrackingFlag> Entities;
        public SortedVector<ulong> Hotspots;

        public EntityTrackingData()
        {
            Entities = new();
            Hotspots = new();
        }
    }

    public class EntityTracker
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Region _region;
        private LinkedList<Iterator> _iterators;
        private Dictionary<PrototypeId, EntityTrackingData> _contextTrackingDataMap;

        public EntityTracker(Region region)
        {
            _region = region;
            _iterators = new();
            _contextTrackingDataMap = new();
        }

        public void ConsiderForTracking(WorldEntity entity)
        {
            if (entity == null || entity.IsTrackable == false) return;

            var entityTracking = entity.TrackingContextMap;
            bool hasOldTracking = entityTracking.Count > 0;

            var interactionTracking = new EntityTrackingContextMap();
            bool hasNewTracking = GameDatabase.InteractionManager.GetEntityContextInvolvement(entity, interactionTracking);

            var newTracking = new EntityTrackingContextMap();
            var oldTracking = new EntityTrackingContextMap();

            if (hasNewTracking)
            {
                foreach (var kvp in interactionTracking)
                    newTracking[kvp.Key] = kvp.Value;

                if (hasOldTracking)
                    foreach (var kvp in entityTracking)
                        if (interactionTracking.ContainsKey(kvp.Key) == false)
                            oldTracking[kvp.Key] = kvp.Value;
            }
            else if (hasOldTracking)
            {
                foreach (var kvp in entityTracking)
                    oldTracking[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in newTracking)
            {
                var contextRef = kvp.Key;
                if (contextRef == PrototypeId.Invalid) continue;

                if (ShouldTrackContext(contextRef))
                {
                    InsertEntityIntoContextMap(contextRef, entity, kvp.Value);
                    entity.ModifyTrackingContext(contextRef, kvp.Value);
                }
            }

            foreach (var kvp in oldTracking)
            {
                var contextRef = kvp.Key;
                if (contextRef == PrototypeId.Invalid) continue;

                RemoveEntityFromContextMap(contextRef, entity);
                entity.ModifyTrackingContext(contextRef, EntityTrackingFlag.None);
            }
        }

        public void RemoveFromTracking(WorldEntity entity)
        {
            foreach (var kvp in entity.TrackingContextMap)
            {
                var contextRef = kvp.Key;
                if (contextRef == PrototypeId.Invalid) continue;
                RemoveEntityFromContextMap(contextRef, entity);
            }
            entity.TrackingContextMap.Clear();
        }

        private bool ShouldTrackContext(PrototypeId contextRef)
        {
            if (_region == null) return false;
            var openProto = GameDatabase.GetPrototype<OpenMissionPrototype>(contextRef);
            if (openProto != null && openProto.IsActiveInRegion(_region.Prototype) == false) return false;
            return true;
        }

        public SortedVector<ulong> HotspotsForContext(PrototypeId contextRef)
        {
            if ( _contextTrackingDataMap.TryGetValue(contextRef, out var data))
                return data.Hotspots;
            return null;
        }

        public void ModifyTrackingContext(WorldEntity entity, PrototypeId contextRef, EntityTrackingFlag flags)
        {
            if (entity == null) return;

            if (flags != EntityTrackingFlag.None)
                InsertEntityIntoContextMap(contextRef, entity, flags);
            else
                RemoveEntityFromContextMap(contextRef, entity);

            entity.ModifyTrackingContext(contextRef, flags);
        }

        private void InsertEntityIntoContextMap(PrototypeId contextRef, WorldEntity entity, EntityTrackingFlag flags)
        {
            if (entity == null || flags == EntityTrackingFlag.None) return;
            if (_contextTrackingDataMap.TryGetValue(contextRef, out var data) == false)
            {
                data = new();
                _contextTrackingDataMap.Add(contextRef, data);
            }

            ulong entityId = entity.Id;
            data.Entities[entityId] = flags;

            if (entity is Hotspot hotspot && hotspot.IsMissionHotspot)
                data.Hotspots.Add(entityId);
        }

        private void RemoveEntityFromContextMap(PrototypeId contextRef, WorldEntity entity)
        {
            if (entity == null) return;
            if (_contextTrackingDataMap.TryGetValue(contextRef, out var data) == false) return;

            var entityId = entity.Id;
            if (data.Entities.TryGetValue(entityId, out _) == false)
            {
                Logger.Warn($"Unable to find entity to remove. ENTITYID={entityId} CONTEXT={GameDatabase.GetFormattedPrototypeName(contextRef)} TRACKER={contextRef}");
                return;
            }
            /*
            if (_iterators.Count > 0)
                foreach (var iterator in _iterators)
                    if (iterator.Entities == data.Entities && iterator.CurrentKey == entityId)
                    {
                        iterator.MoveNext();
                        iterator.Break = true;
                    }
            */
            data.Entities.Remove(entityId);
            data.Hotspots.Remove(entityId);
        }

        public IEnumerable<WorldEntity> Iterate(PrototypeId contextRef,
                EntityTrackingFlag flags = EntityTrackingFlag.None, EntityTrackerOptions options = EntityTrackerOptions.None)
        {
            var iterator = new Iterator(this, contextRef, flags, options);

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
                _iterators.Remove(iterator);
            }
        }

        public class Iterator
        {
            public readonly Dictionary<ulong, EntityTrackingFlag> Entities;
            public ulong CurrentKey { get; private set; }

            private List<ulong> _keys;
            private int _index;
            private readonly EntityTracker _tracker;
            private readonly EntityTrackingFlag _flags;
            private readonly EntityTrackerOptions _options;
            private readonly EntityManager _manager;
            private WorldEntity _current;

            public Iterator(EntityTracker tracker, PrototypeId contextRef, EntityTrackingFlag flags, EntityTrackerOptions options)
            {
                _tracker = tracker;
                _manager = _tracker._region.Game.EntityManager;
                _flags = flags;
                _options = options;
                _keys = new();

                if (contextRef == PrototypeId.Invalid) return;

                _tracker._iterators.AddLast(this);
                if (_tracker._contextTrackingDataMap.TryGetValue(contextRef, out var trackingData) == false) return;

                _index = 0;
                Entities = trackingData.Entities;
                if (Entities == null) return;
                _keys = Entities.Keys.ToList();

                MoveNext();
            }

            public void Advance()
            {
                if (End()) return;
                if (_index < _keys.Count)
                {
                    // update keys
                    if (Entities.Count > _keys.Count)
                        _keys = Entities.Keys.ToList();
                    CurrentKey = _keys[_index];                   
                } 
                _index++;
            }

            public void MoveNext()
            {
                Advance();
                while (IsValid() == false && End() == false)
                    Advance();
            }

            private bool IsValid()
            {
                if (End()) return false;
                if (Entities.TryGetValue(CurrentKey, out var flag) == false) return false; // Break
                if (_flags != 0 && (flag & _flags) == 0) return false;

                var entityId = CurrentKey;
                var entity = _manager.GetEntity<WorldEntity>(entityId);
                if (entity == null) return false;

                if (_options.HasFlag(EntityTrackerOptions.IsDestroyed) == false && entity.IsDestroyed)
                    return false;

                _current = entity;
                return true;
            }

            public bool End() => _index > _keys.Count || Entities == null;
            public WorldEntity Current => IsValid() ? _current : null;

        }
    }
}
