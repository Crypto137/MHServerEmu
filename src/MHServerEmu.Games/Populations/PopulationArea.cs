using MHServerEmu.Core.Collections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class PopulationArea
    {        
        public Game Game { get; }
        public Area Area { get; }
        public PrototypeId PopulationRef { get; }
        public PopulationPrototype PopulationPrototype { get; }
        public Dictionary<Cell, SpawnCell> SpawnCells { get; }
        public PopulationAreaSpawnEvent SpawnEvent { get; private set; }

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
            {
                SpawnEvent = new PopulationAreaSpawnEvent(Area, Area.Region);
                SpawnEvent.PopulationRegisty(PopulationPrototype);
                SpawnEvent.Schedule();
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
            Density = cell.SpawnableArea / PopulationPrototype.PopulationClusterSq * density;
            DensityPeak = cell.SpawnableArea / PopulationPrototype.PopulationClusterSq * densityPeak;
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
