using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class SpawnPopulationRegistry
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public SpawnPopulationRegistry(Game game, Region region)
        {
            Game = game;
            Region = region;
        }

        public Game Game { get; }
        public Region Region { get; }

        public void MissionRegisty(MissionPrototype missionProto)
        {
            if (missionProto == null) return;

            if (missionProto.PopulationSpawns.IsNullOrEmpty() == false)
            {
                foreach (var entry in missionProto.PopulationSpawns)
                {
                    if (entry.RestrictToAreas.IsNullOrEmpty() == false) // check areas
                    {
                        bool foundArea = false;
                        foreach(var areaRef in entry.RestrictToAreas)
                        {
                            if (Region.GetArea(areaRef) != null)
                            {
                                foundArea = true;
                                break;
                            }
                        }
                        if (foundArea == false) continue;

                        // entry.Count; TODO count population

                        if (entry.Population.UsePopulationMarker != PrototypeId.Invalid)
                        {
                            AddPopulationMarker(entry.Population.UsePopulationMarker, entry.Population, entry.RestrictToAreas, entry.RestrictToCells);
                        }

                    }
                }   
            }

        }

        public void AddPopulationMarker(PrototypeId populationMarker, PopulationObjectPrototype population, PrototypeId[] restrictToAreas, AssetId[] restrictToCells)
        {
            //population.UseMarkerOrientation;
            if (population is PopulationEncounterPrototype encounter)
            {
                Logger.Debug($"Spawn {GameDatabase.GetFormattedPrototypeName(GameDatabase.GetDataRefByAsset(encounter.EncounterResource))} => marker {GameDatabase.GetFormattedPrototypeName(populationMarker)}");
            }
            // TODO PopulationObjectPrototype.BuildCluster
        }
    }
}
