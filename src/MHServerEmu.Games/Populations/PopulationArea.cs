using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class PopulationArea
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public const float PopulationClusterSq = 3600.0f; // 60 * 60 , 60 - Average Cluster size
        public Game Game { get; }
        public Area Area { get; }
        public PrototypeId PopulationRef { get; }
        public PopulationPrototype PopulationPrototype { get; }
        public Dictionary<Cell, SpawnCell> SpawnCells { get; }

        public PopulationArea(Area area, PrototypeId populationRef)
        {
            Game = area.Game;
            Area = area;
            PopulationRef = populationRef;

            PopulationPrototype = GameDatabase.GetPrototype<PopulationPrototype>(PopulationRef);
            SpawnCells = new();
        }

        public void Generate()
        {
            if (PopulationPrototype == null || Area.PlayableNavArea <= 0.0f) return;
            if (Area.SpawnableNavArea > 0.0f)
                PopulationRegisty();
        }

        private void PopulationRegisty()
        {
            int objCount = 0;
            int markerCount = 0;
            var populationProto = PopulationPrototype;
            var manager = Area.Region.PopulationManager;
            float spawnableNavArea = Area.SpawnableNavArea;
            //if (populationProto.SpawnMapEnabled || (populationProto.SpawnMapDensityMin > 0.0 && populationProto.SpawnMapDensityMax > 0.0f)) return;
            if (populationProto.Themes == null || populationProto.Themes.List.IsNullOrEmpty()) return;

            List<PrototypeId> areas = new()
            {
                Area.PrototypeDataRef
            };
            List<PrototypeId> cells = new();

            float density = spawnableNavArea / PopulationClusterSq * (populationProto.ClusterDensityPct / 100.0f);
            var themeProto = GameDatabase.GetPrototype<PopulationThemePrototype>(populationProto.Themes.List[0].Object);
            var picker = PopulatePicker(manager.Random, themeProto.Enemies.List);
            while (density > 0.0f && picker.Pick(out var objectProto))
            {
                density -= objectProto.GetAverageSize();
                manager.AddPopulationObject(PrototypeId.Invalid, objectProto, 1, areas, cells, PrototypeId.Invalid);
                objCount++;
            }

            List<PopulationObjectInstancePrototype> encounters = new();
            if (populationProto.GlobalEncounters != null) GetContainedEncounters(populationProto.GlobalEncounters.List, encounters);
            if (themeProto.Encounters != null) GetContainedEncounters(themeProto.Encounters.List, encounters);

            var registry = Area.Region.SpawnMarkerRegistry;
            Dictionary<PrototypeId, SpawnPicker> markerPicker = new();

            foreach (var encounter in encounters)
            {
                var objectProto = GameDatabase.GetPrototype<PopulationObjectPrototype>(encounter.Object);
                var markerRef = objectProto.UsePopulationMarker;
                SpawnPicker spawnPicker;
                if (markerPicker.TryGetValue(markerRef, out var found))
                    spawnPicker = found;
                else
                {
                    int slots = registry.CalcFreeReservation(markerRef, Area.PrototypeDataRef);

                    if (slots == 0)
                        spawnPicker = new(null, 0);
                    else
                    {
                        density = populationProto.GetEncounterDensity(markerRef) / 100.0f;
                        int count = Math.Max(1, (int)(slots * density));
                        spawnPicker = new(new(Game.Random), count);
                    }

                    markerPicker[markerRef] = spawnPicker;
                }

                if (spawnPicker.Count > 0)
                    spawnPicker.Picker.Add(objectProto, encounter.Weight);
            }

            foreach (var kvp in markerPicker)
            {
                var markerRef = kvp.Key;
                var spawnPicker = kvp.Value;
                if (spawnPicker.Picker == null) continue;
                var objectProto = spawnPicker.Picker.Pick();
                manager.AddPopulationObject(markerRef, objectProto, spawnPicker.Count, areas, cells, PrototypeId.Invalid);
                markerCount++;
            }
            Logger.Debug($"Population [{populationProto.SpawnMapDensityMin}][{GameDatabase.GetFormattedPrototypeName(PopulationRef)}][{objCount}][{markerCount}]");
        }

        public static void GetContainedEncounters(PopulationObjectInstancePrototype[] objectList, List<PopulationObjectInstancePrototype> encounters)
        {
            foreach (var objectInstance in objectList)
            {
                if (objectInstance == null || objectInstance.Weight <= 0) continue;
                var proto = GameDatabase.GetPrototype<Prototype>(objectInstance.Object);
                if (proto is PopulationObjectPrototype)
                    encounters.Add(objectInstance);
                else if (proto is PopulationObjectListPrototype populationObjectList)
                    GetContainedEncounters(populationObjectList.List, encounters);
            }
        }

        public static Picker<PopulationObjectPrototype> PopulatePicker(GRandom random, PopulationObjectInstancePrototype[] objectList)
        {
            Picker<PopulationObjectPrototype> picker = new(random);
            foreach (var objectInstance in objectList)
            {
                var objectProto = GameDatabase.GetPrototype<PopulationObjectPrototype>(objectInstance.Object);
                if (objectProto == null) continue;
                int weight = objectInstance.Weight;
                picker.Add(objectProto, weight);
            }

            return picker;
        }

        public void SpawnPopulation(List<PopulationObject> populationObjects)
        {
            if (populationObjects.Count == 0) return;
            var areaRef = Area.PrototypeDataRef;
            Picker<Cell> picker = new(Game.Random);

            foreach (var populationObject in populationObjects)
                if (populationObject.SpawnAreas.Contains(areaRef))
                {
                    picker.Clear();
                    foreach (var kvp in SpawnCells)
                    {
                        SpawnCell spawnCell = kvp.Value;
                        if (spawnCell.CheckDensity(PopulationPrototype))
                            picker.Add(kvp.Key, spawnCell.CellWeight);
                    }
                    if (picker.Pick(out var cell)) populationObject.SpawnInCell(cell);
                }
        }

        public void AddEnemyWeight(Cell cell)
        {
            if (SpawnCells.TryGetValue(cell, out var spawnCell))
                spawnCell.Weight++;
            else
                SpawnCells.Add(cell, new(cell, PopulationPrototype));
        }

        public void UpdateSpawnCell(Cell cell)
        {
            if (SpawnCells.TryGetValue(cell, out var spawnCell))
                spawnCell.CalcDensity(cell, PopulationPrototype);
            else
                SpawnCells.Add(cell, new(cell, PopulationPrototype, 0));
        }
    }

    public class SpawnCell
    {
        public float Density;
        public float DensityPeak;
        public int CellWeight;
        public int Weight;

        public SpawnCell(Cell cell, PopulationPrototype populationProto, int weight = 1)
        {
            CalcDensity(cell, populationProto);
            Weight = weight;
        }

        public bool CheckDensity(PopulationPrototype populationProto)
        {
            return populationProto.ClusterDensityPct > 0 && Density >= 1.0f && Weight <= DensityPeak;
        }

        public void CalcDensity(Cell cell, PopulationPrototype populationProto)
        {
            float density = populationProto.ClusterDensityPct / 100.0f;
            float densityPeak = populationProto.ClusterDensityPeak / 100.0f;
            Density = cell.SpawnableArea / PopulationArea.PopulationClusterSq * density;
            DensityPeak = cell.SpawnableArea / PopulationArea.PopulationClusterSq * densityPeak;
            CellWeight = (int)(cell.SpawnableArea / 1000.0f);
        }
    }

    public class SpawnPicker
    {
        public Picker<PopulationObjectPrototype> Picker;
        public int Count;

        public SpawnPicker(Picker<PopulationObjectPrototype> picker, int count)
        {
            Picker = picker;
            Count = count;
        }
    }
}
