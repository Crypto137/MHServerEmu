using MHServerEmu.Core.Collections;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnScheduler
    {
        public PriorityQueue<PopulationObject, int> ScheduledObjects;
        public SpawnEvent SpawnEvent;

        public SpawnScheduler(SpawnEvent spawnEvent)
        {
            ScheduledObjects = new();
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
                if (populationObject.SpawnByMarker()) // cell.SpawnPopulation(population);
                    UpdateSpawnGroup(populationObject);
        }

        private void UpdateSpawnGroup(PopulationObject populationObject)
        {
            var group = SpawnEvent.PopulationManager.GetSpawnGroup(populationObject.SpawnGroupId);
            if (group != null) group.PopulationObject = populationObject;
        }

        public void ScheduleLocationObject() // Spawn Themes
        {
            if (Pop(out PopulationObject populationObject))
            {
                Picker<Cell> picker = new(SpawnEvent.Game.Random);
                var region = populationObject.SpawnLocation.Region;
                foreach (var area in region.IterateAreas())
                {
                    if (area.IsDynamicArea()) continue;
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
                        UpdateSpawnGroup(populationObject);
                }
            }
        }
    }
}
