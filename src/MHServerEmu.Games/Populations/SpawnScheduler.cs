using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class PopulationObjectQueue
    {
        private readonly PriorityQueue<PopulationObject, int> _queue = new();
        public int Count => _queue.Count;

        public void Push(PopulationObject popObject)
        {
            _queue.Enqueue(popObject, popObject.GetPriority());
        }

        public PopulationObject Pop()
        {
            return _queue.Count > 0 ? _queue.Dequeue() : null;
        }

        public bool CanSpawn(TimeSpan currentTime)
        {
            if (_queue.Count == 0) return false;
            return _queue.Peek().Time <= currentTime;
        }

        public void GetEventTime(ref TimeSpan eventTime)
        {
            if (_queue.Count > 0) eventTime = Clock.Min(eventTime, _queue.Peek().Time);
        }
    }

    public class SpawnMissionObject
    {
        public HashSet<PopulationObject> MissionObjects = new();

        public void Destroy()
        {
            List<SpawnReservation> reservations = ListPool<SpawnReservation>.Instance.Get();
            foreach (var popObj in MissionObjects)
                if (popObj.MarkerReservation != null && popObj.MarkerReservation.State == MarkerState.Pending)
                    reservations.Add(popObj.MarkerReservation);

            MissionObjects.Clear();

            foreach (var reservation in reservations)
                reservation.ResetReservation(false);

            ListPool<SpawnReservation>.Instance.Return(reservations);
        }

        public bool Pending()
        {
            int pendingMarkers = 0;
            foreach(var popObj in MissionObjects)
            {
                var reservation = popObj.MarkerReservation;
                if (reservation != null && reservation.State == MarkerState.Pending)
                    pendingMarkers++;
            }

            return pendingMarkers >= MissionObjects.Count;
        }
    }

    public class SpawnMissionScheduler : SpawnScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Dictionary<PrototypeId, SpawnMissionObject> SpawnMissionObjects { get; }
        public int Priority { get; }

        public SpawnMissionScheduler(SpawnEvent spawnEvent, bool critical) : base(spawnEvent)
        {
            Priority = spawnEvent.Game.Random.Next(100);
            if (critical) Priority += 100;
            SpawnMissionObjects = new();
        }

        public override void Push(PopulationObject popObject)
        {
            base.Push(popObject);

            var markerRef = popObject.MarkerRef;
            if (SpawnMissionObjects.TryGetValue(markerRef, out var missionObject) == false)
            {
                missionObject = new();
                SpawnMissionObjects[markerRef] = missionObject;
            }
            missionObject.MissionObjects.Add(popObject);
        }

        public void ScheduleMissionObjects(bool critical, PrototypeId markerRef)
        {
            var populationObject = Pop(critical);
            if (populationObject != null)
            {
                if (ReservationMarker(populationObject, markerRef) == false)
                    AddFailedObject(populationObject);

                if (CanSpawnMissionMarkers(critical))
                {
                    List<WorldEntity> entities = ListPool<WorldEntity>.Instance.Get();

                    foreach(var missionObject in SpawnMissionObjects.Values)
                        foreach (var spawnObject in missionObject.MissionObjects)
                        {
                            if (spawnObject.SpawnByMarker(entities))
                            {
                                if (PopulationManager.DebugMarker(spawnObject.MarkerRef) && entities.Count > 0)
                                    Logger.Warn($"Spawn MissionObjects {entities[0].RegionLocation.Position} {critical} {Count}");

                                OnSpawnedPopulation(spawnObject);
                            }
                            else
                            {
                                spawnObject.ResetMarker();
                                ListPool<WorldEntity>.Instance.Return(entities);

                                if (PopulationManager.DebugMarker(spawnObject.MarkerRef))
                                    Logger.Warn($"ScheduleMissionObjects failed SpawnByMarker {spawnObject}");

                                return;
                            }

                            entities.Clear();
                        }


                    foreach (var missionObject in SpawnMissionObjects.Values)
                        missionObject.Destroy();

                    SpawnMissionObjects.Clear();

                    ListPool<WorldEntity>.Instance.Return(entities);
                }
            }
        }

        private bool ReservationMarker(PopulationObject populationObject, PrototypeId markerRef)
        {
            if (populationObject == null || populationObject.MarkerRef != markerRef) return false;
            if (SpawnMissionObjects.TryGetValue(markerRef, out var missionObject) == false) return false;
            if (missionObject.Pending()) return false;

            var region = SpawnEvent.Region;
            if (region == null) return false;

            var random = region.Game.Random;
            populationObject.CleanUpSpawnFlags();

            var reservation = region.SpawnMarkerRegistry.ReserveFreeReservation(markerRef, random, populationObject.SpawnLocation, populationObject.SpawnFlags, 0);
            if (reservation == null || reservation.State != MarkerState.Reserved) return false;

            reservation.State = MarkerState.Pending;
            reservation.Object = populationObject.Object;
            reservation.MissionRef = populationObject.MissionRef;

            if (PopulationManager.DebugMarker(markerRef)) Logger.Warn($"ReservationMarker {reservation}");

            populationObject.MarkerReservation = reservation;
            if (missionObject.MissionObjects.Contains(populationObject) == false) return false;

            return true;
        }

        private bool CanSpawnMissionMarkers(bool critical)
        {
            foreach (var missionMarker in SpawnMissionObjects.Values)
                if (missionMarker.Pending() == false) return false;

            return Empty(critical) && SpawnMissionObjects.Count > 0;
        }
    }

    public class SpawnScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PopulationObjectQueue _criticalQueue = new();
        private readonly PopulationObjectQueue _regularQueue = new();

        public HashSet<ulong> SpawnedGroups { get; } 
        public List<PopulationObject> FailedObjects { get; }
        public int Count => _criticalQueue.Count + _regularQueue.Count;
        public bool Any => _criticalQueue.Count > 0 || _regularQueue.Count > 0;

        public SpawnEvent SpawnEvent;

        public SpawnScheduler(SpawnEvent spawnEvent)
        {
            SpawnedGroups = new();
            FailedObjects = new();
            SpawnEvent = spawnEvent;
        }

        public void Destroy()
        {
            var manager = SpawnEvent.PopulationManager;
            if (manager == null) return;

            foreach (var groupId in SpawnedGroups)
                manager.RemoveSpawnGroup(groupId);
        }

        public virtual void Push(PopulationObject popObject)
        {
            if (popObject.Critical)
                _criticalQueue.Push(popObject);
            else
                _regularQueue.Push(popObject);
        }

        public bool Empty(bool critical)
        {
            return critical ? _criticalQueue.Count == 0 : _regularQueue.Count == 0;
        }

        public PopulationObject Pop(bool critical)
        {
            return critical ? _criticalQueue.Pop() : _regularQueue.Pop();
        }

        public PopulationObject PopAny()
        {
            return _criticalQueue.Pop() ?? _regularQueue.Pop();
        }

        public bool CanSpawn(TimeSpan currentTime, bool critical)
        {
            if (critical)
                return _criticalQueue.CanSpawn(currentTime);
            else
                return _regularQueue.CanSpawn(currentTime);
        }

        public bool CanAnySpawn(TimeSpan currentTime)
        {
            return _criticalQueue.CanSpawn(currentTime) || _regularQueue.CanSpawn(currentTime);
        }

        public void GetMinEventTime(ref TimeSpan eventTime)
        {
            _criticalQueue.GetEventTime(ref eventTime);
            _regularQueue.GetEventTime(ref eventTime);
        }

        public void ScheduleMarkerObject(bool critical) // Spawn Entity from Missions, MetaStates
        {
            var populationObject = Pop(critical);
            if (populationObject != null)
            {
                List<WorldEntity> entities = ListPool<WorldEntity>.Instance.Get();

                if (populationObject.SpawnByMarker(entities))
                {
                    if (PopulationManager.DebugMarker(populationObject.MarkerRef) && entities.Count > 0) 
                        Logger.Warn($"Spawn MarkerObject {entities[0].RegionLocation.Position} {_criticalQueue.Count} {_regularQueue.Count}");
                    
                    OnSpawnedPopulation(populationObject);
                }
                else if (populationObject.RemoveOnSpawnFail == false)
                    AddFailedObject(populationObject);

                ListPool<WorldEntity>.Instance.Return(entities);
            }
        }

        public void OnSpawnedPopulation(PopulationObject populationObject)
        {
            if (populationObject == null || populationObject.SpawnGroupId == SpawnGroup.InvalidId) return;

            var group = SpawnEvent.PopulationManager.GetSpawnGroup(populationObject.SpawnGroupId);
            if (group != null)
            {
                group.PopulationObject = populationObject;
                SpawnedGroups.Add(populationObject.SpawnGroupId);
            }
            SpawnEvent.OnSpawnedPopulation();
        }

        public void ScheduleLocationObject(bool critical) // Spawn Themes
        {
            var populationObject = Pop(critical);
            if (populationObject != null)
            {
                var picker = CellPicker(populationObject);
                if (picker.Pick(out var cell))
                {
                    bool spawned;
                    if (populationObject.Position == null)
                        spawned = populationObject.SpawnInCell(cell);
                    else 
                        spawned = populationObject.SpawnInPosition(populationObject.Position.Value);

                    if (spawned)
                        OnSpawnedPopulation(populationObject);
                    else
                    {
                        if (populationObject.RemoveOnSpawnFail)
                            populationObject.SpawnHeat?.Return();
                        else
                            AddFailedObject(populationObject);
                    }
                }
            }
        }

        private static Picker<Cell> CellPicker(PopulationObject populationObject)
        {
            Picker<Cell> picker = new(populationObject.Random);
            var region = populationObject.SpawnLocation.Region;

            IEnumerable<Area> spawnAreas;
            if (populationObject.SpawnEvent is PopulationAreaSpawnEvent popEvent)
                spawnAreas = [popEvent.Area];
            else
                spawnAreas = region.IterateAreas();

            foreach (var area in spawnAreas)
            {
                var popArea = area.PopulationArea;
                if (popArea == null) continue;
                foreach (var kvp in popArea.SpawnCells)
                {
                    var cell = kvp.Key;
                    if (populationObject.SpawnLocation.SpawnableCell(cell) == false) continue;
                    SpawnCell spawnCell = kvp.Value;
                    if (spawnCell.CheckDensity(popArea.PopulationPrototype, populationObject.RemoveOnSpawnFail))
                        picker.Add(cell, spawnCell.CellWeight);
                }
            }
            return picker;
        }

        public void AddFailedObject(PopulationObject populationObject)
        {
            if (PopulationManager.DebugMarker(populationObject.MarkerRef)) Logger.Trace($"Failed Spawn {populationObject}");
            FailedObjects.Add(populationObject);
        }

        public void PushFailedObjects()
        {
            if (FailedObjects.Count == 0) return;
            // if (PopulationManager.Debug) Logger.Trace($"PushFailedObjects [{FailedObjects.Count}]");

            foreach (var popObject in FailedObjects)
                Push(popObject);

            FailedObjects.Clear();
        }
    }

    public class SpawnSpecScheduler
    {
        private readonly List<SpawnSpec> _specs = new ();

        public SpawnSpecScheduler() { }

        public void Schedule(SpawnSpec spec)
        {
            _specs.Add(spec);
        }

        public void Spawn(bool forceSpawn)
        {
            foreach (var spec in _specs)
                Respawn(spec, forceSpawn);
        }

        private static void Respawn(SpawnSpec spec, bool forseSpawn)
        {
            if (spec == null) return;

            var worldEntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(spec.EntityRef);
            if (worldEntityProto != null && worldEntityProto.IsLiveTuningEnabled() == false) return;

            var activeEntity = spec.ActiveEntity;
            if (activeEntity != null)
            {
                if (activeEntity.Properties[PropertyEnum.PlaceableDead]) return;
                int health = activeEntity.Properties[PropertyEnum.Health];
                if (health >= activeEntity.Properties[PropertyEnum.HealthMax]) return;
            }

            if (forseSpawn)
            {
                var popGlobals = GameDatabase.GlobalsPrototype.PopulationGlobalsPrototype;
                if (popGlobals == null) return;
                var spawnTime = TimeSpan.FromMilliseconds(popGlobals.DestructiblesForceSpawnMS);
                var respawnTime = spec.Game.CurrentTime - spec.SpawnedTime;
                if (respawnTime <= spawnTime) return;
            }

            spec.Respawn();
        }
    }
}
