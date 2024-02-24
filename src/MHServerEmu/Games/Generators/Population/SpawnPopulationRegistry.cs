using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Generators.Population
{
    public class PopulationMarker
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public PrototypeId MarkerRef;
        public PrototypeId MissionRef;
        public GRandom Random;
        public PropertyCollection Properties;
        public SpawnFlags SpawnFlags;
        public PopulationObjectPrototype Object;
        public PrototypeId[] SpawnAreas;
        public AssetId[] SpawnCells;
        public int Count;       

        public void Spawn(Cell cell)
        {
            // TODO add fast check markerRef for cell
            if (Count == 0) return;
            Region region = cell.GetRegion();
            SpawnMarkerRegistry registry = region.SpawnMarkerRegistry;
            SpawnReservation reservation = registry.ReserveFreeReservation(MarkerRef, Random, cell, SpawnAreas, SpawnCells);
            if (reservation != null)
            {               
                //Logger.Warn($"{GameDatabase.GetFormattedPrototypeName(MissionRef)} {pos.ToStringFloat()}");
                ClusterGroup clusterGroup = new(region, Random, Object, null, Properties, SpawnFlags);
                clusterGroup.Initialize();
                // set group position
                var pos = reservation.GetRegionPosition();
                clusterGroup.SetParentRelativePosition(pos);
                clusterGroup.SetParentRelativeOrientation(reservation.MarkerRot); // can be random?
                // spawn Entity from Group
                clusterGroup.Spawn();
                Count--;
            }
        }
    }

    public class SpawnPopulationRegistry
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Game Game { get; }
        public Region Region { get; }

        public List<PopulationMarker> PopulationMarkers;

        public SpawnPopulationRegistry(Game game, Region region)
        {
            Game = game;
            Region = region;
            PopulationMarkers = new();
        }

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
                        //for (var i = 0; i < entry.Count; i++)
                        AddPopulationMarker(entry.Population.UsePopulationMarker, entry.Population, (int)entry.Count, entry.RestrictToAreas, entry.RestrictToCells, missionProto.DataRef);
                    }
                }   
            }

        }

        public void AddPopulationMarker(PrototypeId populationMarkerRef, PopulationObjectPrototype population, int count, PrototypeId[] restrictToAreas, AssetId[] restrictToCells, PrototypeId missionRef)
        {
            //check marker exist population.UseMarkerOrientation;
            //Logger.Warn($"SpawnMarker[{count}] {GameDatabase.GetFormattedPrototypeName(populationMarkerRef)}");
            GRandom random = Game.Random;
            PopulationMarker populationMarker = new()
            {
                MarkerRef = populationMarkerRef,
                MissionRef = missionRef,
                Random = random,
                Properties = new PropertyCollection(),
                SpawnFlags = SpawnFlags.None,
                Object = population,
                SpawnAreas = restrictToAreas,
                SpawnCells = restrictToCells,
                Count = count
            };
            PopulationMarkers.Add(populationMarker);
        }
    }
}
