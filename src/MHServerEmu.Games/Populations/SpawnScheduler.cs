using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Time;
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

    public class SpawnScheduler
    {
        private readonly PopulationObjectQueue _criticalQueue = new();
        private readonly PopulationObjectQueue _regularQueue = new();

        public Queue<PopulationObject> FailedObjects { get; }
        public int Count => _criticalQueue.Count + _regularQueue.Count;
        public bool Any => _criticalQueue.Count > 0 || _regularQueue.Count > 0;

        public SpawnEvent SpawnEvent;

        public SpawnScheduler(SpawnEvent spawnEvent)
        {
            FailedObjects = new();
            SpawnEvent = spawnEvent;
        }

        public void Push(PopulationObject popObject)
        {
            if (popObject.Critical)
                _criticalQueue.Push(popObject);
            else
                _regularQueue.Push(popObject);
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
                if (populationObject.SpawnByMarker()) // cell.SpawnPopulation(population);
                    OnSpawnedPopulation(populationObject);
                else FailedObjects.Enqueue(populationObject);
            }
        }

        private void OnSpawnedPopulation(PopulationObject populationObject)
        {
            if (populationObject == null || populationObject.SpawnGroupId == SpawnGroup.InvalidId) return;

            var group = SpawnEvent.PopulationManager.GetSpawnGroup(populationObject.SpawnGroupId);
            if (group != null) group.PopulationObject = populationObject;
            SpawnEvent.OnSpawnedPopulation();
        }

        public void ScheduleLocationObject() // Spawn Themes
        {
            var populationObject = PopAny();
            if (populationObject != null)
            {
                var picker = CellPicker(populationObject);
                if (picker.Pick(out var cell))
                {
                    bool spawned;
                    if (populationObject.Position == null)
                        spawned = populationObject.SpawnInCell(cell); // PopulationArea.SpawnPopulation(PopulationObjects);
                    else 
                        spawned = populationObject.SpawnInPosition(populationObject.Position.Value);

                    if (spawned)
                        OnSpawnedPopulation(populationObject);
                }
            }
        }

        private static Picker<Cell> CellPicker(PopulationObject populationObject)
        {
            Picker<Cell> picker = new(populationObject.Random);
            if (populationObject.SpawnEvent is not PopulationAreaSpawnEvent popEvent) return picker;

            var popArea = popEvent.Area.PopulationArea;
            if (popArea == null) return picker;

            foreach (var kvp in popArea.SpawnCells)
            {
                var cell = kvp.Key;
                if (populationObject.SpawnLocation.SpawnableCell(cell) == false) continue;
                SpawnCell spawnCell = kvp.Value;
                if (spawnCell.CheckDensity(popArea.PopulationPrototype, populationObject.RemoveOnSpawnFail))
                    picker.Add(cell, spawnCell.CellWeight);
            }
            return picker;
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
