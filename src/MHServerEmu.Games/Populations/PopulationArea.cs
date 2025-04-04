using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class PopulationArea
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Game Game { get; }
        public Area Area { get; }
        public PrototypeId PopulationRef { get; }
        public PopulationPrototype PopulationPrototype { get; }
        public Dictionary<Cell, SpawnCell> SpawnCells { get; }
        public PopulationAreaSpawnEvent SpawnEvent { get; private set; }
        public int PlayerCount { get; private set; }

        private readonly Dictionary<PrototypeId, List<PopulationObjectInstancePrototype>> _themeDict = new();

        public PopulationArea(Area area, PrototypeId populationRef)
        {
            Game = area.Game;
            Area = area;
            PopulationRef = populationRef;
            PlayerCount = 0;

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

        public void RemoveEnemyWeight(Cell cell)
        {
            if (SpawnCells.TryGetValue(cell, out var spawnCell))
                spawnCell.Weight--;
        }

        public void UpdateSpawnCell(Cell cell)
        {
            if (SpawnCells.TryGetValue(cell, out var spawnCell))
                spawnCell.CalcDensity(cell, PopulationPrototype);
            else
                SpawnCells.Add(cell, new(cell, PopulationPrototype, 0));
        }

        public float CellDencity(Cell cell)
        {
            if (SpawnCells.TryGetValue(cell, out var spawnCell))
                return spawnCell.WeightDencity;
            return 0.0f;
        }

        public void UpdateSpawnMap(Vector3 position)
        {
            var spawnMap = Area.SpawnMap;
            if (spawnMap.ProjectAreaPosition(position, out Point2 coord) == false) return;

            var random = Game.Random;

            var picker = spawnMap.Picker;
            picker.AddHorizon(coord, spawnMap.Horizon, false);
            int spawned = 0;
            int distributed = 0;
            while (picker.Pick(out int index))
            {
                if (index < 0) continue;
                var heatData = spawnMap.GetHeatData(index);
                if (SpawnMap.HasFlags(heatData)) continue;
                int heat = SpawnMap.GetHeat(heatData);
                if (heat == 0) continue;

                bool spawn = false;
                if (random.NextFloat() < spawnMap.MaxChance)
                {
                    // LiveTuning AreaMobSpawnHeat
                    float liveHeat = heat * LiveTuningManager.GetLiveAreaTuningVar(Area.Prototype, AreaTuningVar.eATV_AreaMobSpawnHeat);
                    int maxHeat = (int)HeatData.Max + 1;
                    heat = MathHelper.ClampNoThrow((int)liveHeat, 0, maxHeat);

                    spawn = random.Next(maxHeat) <= heat;
                }

                spawn &= spawnMap.ProjectToPosition(index, out Vector3 spawnPosition);

                if (spawn)
                {
                    float crowdSupression = spawnMap.CalcCrowdSupression(spawnPosition);
                    spawn = random.NextFloat() >= crowdSupression;
                }

                if (spawn)
                {
                    // spawn population
                    SpawnHeatPopulation(spawnPosition, index, random, spawnMap);
                    spawned++;
                }
                else
                {
                    // distribute Heat
                    spawnMap.DistributeHeat(index, coord);
                    distributed++;
                }
            }
            // Logger.Info($"UpdateSpawnMap spawned {spawned} distributed {distributed}");
        }

        private void SpawnHeatPopulation(Vector3 position, int index, GRandom random, SpawnMap spawnMap)
        {
            var cell = Area.GetCellAtPosition(position);
            if (cell == null) return;

            var themeRef = cell.GetPopulationTheme(PopulationPrototype);
            var enemies = PickThemeEnemies(random, themeRef);
            if (enemies == null || enemies.Count == 0) return;

            var picker = PopulationObject.PopulatePicker(random, enemies.ToArray());
            if (picker.Pick(out var objectProto))
            {
                int heat = spawnMap.PickBleedHeat(index);

                if (CellDencity(cell) <= 1.0f)
                {
                    SpawnEvent.AddHeatObject(position, objectProto, new(spawnMap, heat));
                    SpawnEvent.Schedule();
                }
                else spawnMap.PoolHeat(heat);
            }
        }

        private List<PopulationObjectInstancePrototype> PickThemeEnemies(GRandom random, PrototypeId themeRef)
        {
            if (_themeDict.TryGetValue(themeRef, out var enemies)) return enemies;

            enemies = new();
             _themeDict.Add(themeRef, enemies);

            var themeProto = GameDatabase.GetPrototype<PopulationThemePrototype>(themeRef);
            if (themeProto == null) return null;

            if (themeProto.EnemyPicks > 0)
                PopulationObject.PickEnemies(random, themeProto.EnemyPicks, themeProto.Enemies.List, enemies);
            else
                PopulationObject.GetContainedEncounters(themeProto.Enemies.List, enemies);

            return enemies;
        }

        public void OnPlayerEntered()
        {
            PlayerCount++;
            Area.SpawnMap?.UpdateMap();
        }

        public void OnPlayerLeft()
        {
            PlayerCount--;
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

        public float WeightDencity => Density > 0.0f ? Weight / Density : 1.0f;

        public bool CheckDensity(PopulationPrototype populationProto, bool removeOnSpawnFail)
        {
            if (removeOnSpawnFail)
                return populationProto.ClusterDensityPct > 0 && Density >= 1.0f && Weight <= DensityPeak;
            else
                return CellWeight > 0;
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

}
