using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.Templates;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using static MHServerEmu.Games.Populations.PopulationManager;

namespace MHServerEmu.Games.Populations
{
    public class PopulationManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Game Game { get; }
        public Region Region { get; }
        public GRandom Random { get; }
        public Dictionary<PrototypeId, MarkerEventScheduler> MarkerSchedulers { get; }
        public List<SpawnScheduler> LocationSchedulers { get; }
        private List<SpawnEvent> _spawnEvents { get; }

        private EventGroup _pendingEvents = new();
        private ulong _blackOutId;
        private BlackOutSpatialPartition _blackOutSpatialPartition;
        private Dictionary<ulong, BlackOutZone> _blackOutZones;
        private Dictionary<KeyValuePair<PrototypeId, PrototypeId>, ulong> _encounterSpawnPhases;
        private ulong NextBlackOutId() => _blackOutId++;

        private ulong _nextSpawnGroupId;
        private Dictionary<ulong, SpawnGroup> _spawnGroups;
        private ulong NextSpawnGroupId() => _nextSpawnGroupId++;

        private ulong _nextSpawnSpecId;
        private Dictionary<ulong, SpawnSpec> _spawnSpecs;
        private ulong NextSpawnSpecId() => _nextSpawnSpecId++;

        private EventPointer<LocationSpawnEvent> _locationSpawnEvent = new();

        public PopulationManager(Game game, Region region)
        {
            Game = game;
            Region = region;
            Random = new(region.RandomSeed);
            MarkerSchedulers = new();
            LocationSchedulers = new();
            _encounterSpawnPhases = new();
            _blackOutZones = new();
            _blackOutId = 1;
            _spawnGroups = new();
            _nextSpawnGroupId = 1;
            _spawnSpecs = new();
            _spawnEvents = new();
            _nextSpawnSpecId = 1;
        }

        public void Deallocate()
        {
            var scheduler = Game.GameEventScheduler;
            scheduler.CancelAllEvents(_pendingEvents);
            _encounterSpawnPhases.Clear();
            _blackOutZones.Clear();
            _spawnEvents.Clear();
            // TODO clear Schedulers?
            MarkerSchedulers.Clear();
            LocationSchedulers.Clear();
        }

        public void AddSpawnEvent(SpawnEvent spawnEvent)
        {
            if (_spawnEvents.Contains(spawnEvent) == false) 
                _spawnEvents.Add(spawnEvent);
        }

        public void ScheduleSpawnEvent(SpawnEvent spawnEvent)
        {
            foreach (var kvp in spawnEvent.SpawnMarkerSchedulers)
            {
                var markerRef = kvp.Key;
                var markerScheduler = kvp.Value;
                if (MarkerSchedulers.ContainsKey(markerRef) == false)
                    MarkerSchedulers[markerRef] = new();
                MarkerSchedulers[markerRef].SpawnSchedulers.Add(markerScheduler);
                MarkerSchedule(markerRef);
            }

            if (spawnEvent.SpawnLocationSchedulers.Count > 0)
            {
                LocationSchedulers.AddRange(spawnEvent.SpawnLocationSchedulers.Values);
                LocationSchedule();
            }
        }

        private void LocationSchedule()
        {
            var scheduler = Game.GameEventScheduler;
            TimeSpan timeOffset = TimeSpan.FromSeconds(500); // TODO calc Max timeOffset
            if (_locationSpawnEvent.IsValid == false)
            {
                scheduler.ScheduleEvent(_locationSpawnEvent, timeOffset, _pendingEvents);
                _locationSpawnEvent.Get().Initialize(this);
            }
            else if (_locationSpawnEvent.Get().FireTime > Game.CurrentTime + timeOffset)
                scheduler.RescheduleEvent(_locationSpawnEvent, timeOffset);
        }

        private void MarkerSchedule(PrototypeId markerRef)
        {
            var scheduler = Game.GameEventScheduler;
            if (scheduler == null) return;
            if (MarkerSchedulers.TryGetValue(markerRef, out var markerEventScheduler) == false) return;
            if (Region.SpawnMarkerRegistry.CalcFreeReservation(markerRef) == 0) return;
            
            var markerEvent = markerEventScheduler.MarkerSpawnEvent;
            TimeSpan timeOffset = TimeSpan.FromSeconds(20); // TODO calc Max timeOffset
            if (markerEvent.IsValid == false)
            {
                scheduler.ScheduleEvent(markerEvent, timeOffset, _pendingEvents);
                markerEvent.Get().Initialize(this, markerRef);
            }
            else if (markerEvent.Get().FireTime > Game.CurrentTime + timeOffset)
                scheduler.RescheduleEvent(markerEvent, timeOffset);
        }

        private void ScheduleLocationObject()
        {
            Picker<SpawnScheduler> schedulerPicker = new(Game.Random);
            foreach (var scheduler in LocationSchedulers)
                if (scheduler.ScheduledObjects.Count > 0)
                    schedulerPicker.Add(scheduler);

            while (schedulerPicker.Empty() == false)
            {
                schedulerPicker.PickRemove(out var scheduler);
                if (scheduler.ScheduledObjects.Count > 0)
                    scheduler.ScheduleLocationObject();
                if (scheduler.ScheduledObjects.Count > 0)
                    schedulerPicker.Add(scheduler);
            }

            LocationSchedule();
        }

        private void ScheduleMarkerObject(PrototypeId markerRef)
        {
            if (MarkerSchedulers.TryGetValue(markerRef, out var markerEventScheduler)
                && Region.SpawnMarkerRegistry.CalcFreeReservation(markerRef) > 0)
                foreach (var scheduler in markerEventScheduler.SpawnSchedulers)
                    if (scheduler.ScheduledObjects.Count > 0)
                        scheduler.ScheduleMarkerObject();

            MarkerSchedule(markerRef);
        }

        public void SpawnObject(PopulationObjectPrototype popObject, RegionLocation location, PropertyCollection properties, SpawnFlags spawnFlags, WorldEntity spawner, out List<WorldEntity> entities)
        {
            var region = location.Region;
            GRandom random = Game.Random;
            SpawnTarget spawnTarget = new(region);
            if (popObject.UsePopulationMarker != PrototypeId.Invalid)
            {
                spawnTarget.Type = SpawnTargetType.Marker;
                spawnTarget.Cell = location.Cell;
            }
            else
            {
                spawnTarget.Type = SpawnTargetType.Spawner;
                spawnTarget.Location = location;
                spawnTarget.SpawnerProto = spawner.Prototype as SpawnerPrototype;
            }
            var spawnLocation = new SpawnLocation(Region, location.Area);

            PopulationObject populationObject = new()
            {
                MarkerRef = popObject.UsePopulationMarker,
                Random = random,
                Properties = properties,
                SpawnFlags = spawnFlags,
                Object = popObject,
                Spawner = spawner,
                SpawnLocation = spawnLocation,
                Count = 1
            };
            populationObject.SpawnObject(spawnTarget, out entities);
        }

        public ulong SpawnBlackOutZone(Vector3 position, float radius, PrototypeId missionRef)
        {
            var id = NextBlackOutId();
            BlackOutZone zone = new(id, position, radius, missionRef);
            _blackOutZones[id] = zone;
            _blackOutSpatialPartition.Insert(zone);
            // TODO BlackOutZonesRebuild
            Region.SpawnMarkerRegistry.AddBlackOutZone(zone);
            return id;
        }

        public IEnumerable<BlackOutZone> IterateBlackOutZoneInVolume<B>(B bound) where B : IBounds
        {
            if (_blackOutSpatialPartition != null)
                return _blackOutSpatialPartition.IterateElementsInVolume(bound);
            else
                return Enumerable.Empty<BlackOutZone>();
        }

        public void InitializeSpacialPartition(in Aabb bound)
        {
            if (_blackOutSpatialPartition != null) return;
            _blackOutSpatialPartition = new(bound);

            foreach (var zone in _blackOutZones)
                if (zone.Value != null) _blackOutSpatialPartition.Insert(zone.Value);
        }

        public bool InBlackOutZone(Vector3 position, float radius, PrototypeId missionRef)
        {
            Sphere sphere = new(position, radius);
            if (missionRef != PrototypeId.Invalid)
            {
                foreach (var zone in IterateBlackOutZoneInVolume(sphere))
                    if (zone.MissionRef != missionRef)
                        return false;
            }
            else if (IterateBlackOutZoneInVolume(sphere).Any() == false)
                return false;

            return true;
        }

        public SpawnGroup CreateSpawnGroup()
        {
            ulong id = NextSpawnGroupId();
            SpawnGroup group = new(id, this);
            _spawnGroups[id] = group;
            return group;
        }

        public SpawnGroup GetSpawnGroup(ulong groupId)
        {
            if (_spawnGroups.TryGetValue(groupId, out var group))
                return group;
            return null;
        }

        private void RemoveSpawnGroup(ulong groupId)
        {
            if (_spawnGroups.TryGetValue(groupId, out var group))
            {
                _spawnGroups.Remove(groupId);
                group.Destroy();
            }
        }

        public SpawnSpec CreateSpawnSpec(SpawnGroup group)
        {
            ulong id = NextSpawnSpecId();
            SpawnSpec spec = new(id, group);
            group.AddSpec(spec);
            _spawnSpecs[id] = spec;
            return spec;
        }

        public void RemoveSpawnSpec(ulong id)
        {
            _spawnSpecs.Remove(id);
        }

        public void DespawnSpawnGroups(PrototypeId missionRef)
        {
            List<SpawnGroup> despawnGroups = new ();
            foreach (var group in _spawnGroups.Values)
                if (group!= null && group.MissionRef == missionRef)
                    despawnGroups.Add(group);

            foreach (var despawnGroup in despawnGroups)
                RemoveSpawnGroup(despawnGroup.Id);
        }

        public void ResetEncounterSpawnPhase(PrototypeId missionRef)
        {
            List<KeyValuePair<PrototypeId, PrototypeId>> keysToRemove = new ();
            foreach (var pair in _encounterSpawnPhases)
                if (pair.Key.Value == missionRef)
                    keysToRemove.Add(pair.Key);

            foreach (var key in keysToRemove)
                _encounterSpawnPhases.Remove(key);
        }

        public void SpawnEncounterPhase(int encounterPhase, PrototypeId encounterRef, PrototypeId missionRef)
        {
            if (encounterRef == PrototypeId.Invalid || encounterPhase == 0) return;
            KeyValuePair<PrototypeId, PrototypeId> key = new(encounterRef, missionRef);
            if (_encounterSpawnPhases.TryGetValue(key, out var _)) return;
            _encounterSpawnPhases[key] = 1ul << encounterPhase;
            foreach(var group in _spawnGroups.Values)
            {
                if (group == null) continue;
                if (group.EncounterRef == encounterRef)
                    if (missionRef == PrototypeId.Invalid || missionRef == group.MissionRef)
                        foreach(var spec in group.Specs)
                        {
                            if (spec == null) continue;
                            spec.Properties[PropertyEnum.EncounterResource] = encounterRef;
                            if (spec.EncounterSpawnPhase == encounterPhase)
                                spec.Spawn();
                        }
            }
        }

        public bool CheckEncounterPhase(int encounterPhase, PrototypeId encounterRef, PrototypeId missionRef)
        {
            if (encounterPhase == 0) return true;
            KeyValuePair<PrototypeId, PrototypeId> key = new(encounterRef, missionRef);

            if (_encounterSpawnPhases.TryGetValue(key, out var foundPhase)) 
                if ((foundPhase & (1ul << encounterPhase)) != 0 ) return true;

            key = new(encounterRef, PrototypeId.Invalid);
            if (_encounterSpawnPhases.TryGetValue(key, out var noMissionPhase))
                if ((noMissionPhase & (1ul << encounterPhase)) != 0) return true;

            return false;
        }

        public class MarkerEventScheduler
        {
            public List<SpawnScheduler> SpawnSchedulers = new();
            public EventPointer<MarkerSpawnEvent> MarkerSpawnEvent = new();
        }

        public class LocationSpawnEvent : CallMethodEvent<PopulationManager>
        {
            protected override CallbackDelegate GetCallback() => (manager) => manager.ScheduleLocationObject();
        }

        public class MarkerSpawnEvent : CallMethodEventParam1<PopulationManager, PrototypeId>
        {
            protected override CallbackDelegate GetCallback() => (manager, markerRef) => manager.ScheduleMarkerObject(markerRef);
        }

    }
}
