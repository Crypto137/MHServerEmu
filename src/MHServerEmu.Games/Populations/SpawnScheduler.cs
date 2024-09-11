using MHServerEmu.Core.Collections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnScheduler
    {
        public PriorityQueue<PopulationObject, int> ScheduledObjects;
        public SpawnEvent SpawnEvent;
        public Queue<PopulationObject> FailedObjects;

        public SpawnScheduler(SpawnEvent spawnEvent)
        {
            ScheduledObjects = new();
            FailedObjects = new();
            SpawnEvent = spawnEvent;
        }

        public void Push(PopulationObject populationObject)
        {
            ScheduledObjects.Enqueue(populationObject, populationObject.GetPriority());
        }

        public bool Pop(out PopulationObject populationObject)
        {
            populationObject = null;
            if (ScheduledObjects.Count == 0) return false;
            populationObject = ScheduledObjects.Dequeue();
            return true;
        }

        public void ScheduleMarkerObject() // Spawn Entity from Missions, MetaStates
        {
            if (Pop(out PopulationObject populationObject))
            {
                if (populationObject.SpawnByMarker()) // cell.SpawnPopulation(population);
                    OnSpawnedPopulation(populationObject);
                else FailedObjects.Enqueue(populationObject);
            }
        }

        private void OnSpawnedPopulation(PopulationObject populationObject)
        {
            if (populationObject == null || populationObject.SpawnGroupId == 0) return;

            var group = SpawnEvent.PopulationManager.GetSpawnGroup(populationObject.SpawnGroupId);
            if (group != null) group.PopulationObject = populationObject;
            SpawnEvent.OnSpawnedPopulation();
        }

        public void ScheduleLocationObject() // Spawn Themes
        {
            if (Pop(out PopulationObject populationObject))
            {
                Picker<Cell> picker = new(SpawnEvent.Game.Random);
                var region = populationObject.SpawnLocation.Region;
                foreach (var area in region.IterateAreas())
                {
                    if (area.IsDynamicArea) continue;
                    var popArea = area.PopulationArea;
                    if (popArea == null) continue;
                    foreach (var kvp in popArea.SpawnCells)
                    {
                        var cell = kvp.Key;
                        if (populationObject.SpawnLocation.SpawnableCell(cell) == false) continue;
                        SpawnCell spawnCell = kvp.Value;
                        if (spawnCell.CheckDensity(popArea.PopulationPrototype))
                            picker.Add(cell, spawnCell.CellWeight);
                    }
                }

                if (picker.Empty() == false)
                {
                    var cell = picker.Pick();
                    if (populationObject.SpawnInCell(cell)) // PopulationArea.SpawnPopulation(PopulationObjects);
                        OnSpawnedPopulation(populationObject);
                }
            }
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
