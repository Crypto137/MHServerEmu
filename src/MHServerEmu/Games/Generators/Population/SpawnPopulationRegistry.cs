using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
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
        public List<PrototypeId> SpawnAreas;
        public List<PrototypeId> SpawnCells;
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
                reservation.Object = Object;
                reservation.MissionRef = MissionRef;
                
                //Logger.Warn($"{GameDatabase.GetFormattedPrototypeName(MissionRef)} {GameDatabase.GetFormattedPrototypeName(MarkerRef)} {pos}");
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

            if (missionProto.PopulationSpawns.HasValue())
            {
                foreach (var entry in missionProto.PopulationSpawns)
                {
                    //if (missionProto.DesignState == DesignWorkflowState.NotInGame) 
                    //    Logger.Debug($"Mission [{missionProto.DesignState}] {GameDatabase.GetFormattedPrototypeName(missionProto.DataRef)} = {missionProto.DataRef} {GameDatabase.GetFormattedPrototypeName(entry.Population.UsePopulationMarker)}");
                    if (entry.RestrictToAreas.HasValue()) // check areas
                    {
                        bool foundArea = false;
                        foreach (var areaRef in entry.RestrictToAreas)
                        {
                            if (Region.GetArea(areaRef) != null)
                            {
                                foundArea = true;
                                break;
                            }
                        }
                        if (foundArea == false) continue;

                        List<PrototypeId> regionAreas = new();
                        foreach (var areaRef in entry.RestrictToAreas)
                            regionAreas.Add(areaRef);
                        // entry.Count; TODO count population
                        //for (var i = 0; i < entry.Count; i++)
                        AddPopulationMarker(entry.Population.UsePopulationMarker, entry.Population, (int)entry.Count, regionAreas, AssetsToList(entry.RestrictToCells), missionProto.DataRef);
                    }
                    else if (entry.RestrictToRegions.HasValue()) // No areas but have Region
                    {
                        List<PrototypeId> regionAreas = new ();
                        foreach (var area in Region.IterateAreas())
                            regionAreas.Add(area.PrototypeDataRef);

                        AddPopulationMarker(entry.Population.UsePopulationMarker, entry.Population, (int)entry.Count, regionAreas, AssetsToList(entry.RestrictToCells), missionProto.DataRef);
                    }
                }   
            }

        }

        public static List<PrototypeId> AssetsToList(AssetId[] assets)
        {
            if (assets.IsNullOrEmpty()) return new();
            List<PrototypeId> list = new();
            foreach (var asset in assets)
                list.Add(GameDatabase.GetDataRefByAsset(asset));
            return list;
        }

        public PopulationMarker AddPopulationMarker(PrototypeId populationMarkerRef, PopulationObjectPrototype population, int count, List<PrototypeId> restrictToAreas, List<PrototypeId> restrictToCellsRef, PrototypeId missionRef)
        {
            //check marker exist population.UseMarkerOrientation;
            //Logger.Warn($"SpawnMarker[{count}] {GameDatabase.GetFormattedPrototypeName(populationMarkerRef)}");
            GRandom random = Game.Random;
            PropertyCollection properties = null;
            if (missionRef != PrototypeId.Invalid)
            {
                properties = new PropertyCollection();
                properties[PropertyEnum.MissionPrototype] = missionRef;
            }
            PopulationMarker populationMarker = new()
            {
                MarkerRef = populationMarkerRef,
                MissionRef = missionRef,
                Random = random,
                Properties = properties,
                SpawnFlags = SpawnFlags.None,
                Object = population,
                SpawnAreas = restrictToAreas,
                SpawnCells = restrictToCellsRef,
                Count = count
            };
            
            PopulationMarkers.Add(populationMarker);
            return populationMarker;
        }

        public void MetaStateRegisty(PrototypeId prototypeId)
        {
            var popProto = GameDatabase.GetPrototype<MetaStatePopulationMaintainPrototype>(prototypeId);
            if (popProto != null && popProto.PopulationObjects.HasValue())
            {
                List<PrototypeId> regionAreas = new();
                List<PrototypeId> regionCell;
                if (popProto.RestrictToAreas.IsNullOrEmpty())
                {
                    foreach (var area in Region.IterateAreas())
                        regionAreas.Add(area.PrototypeDataRef);
                    regionCell = new();
                    foreach (var cell in Region.Cells)
                        if (cell.Area.IsDynamicArea() == false)
                            regionCell.Add(cell.PrototypeId);
                }
                else
                {
                    foreach (var areaRef in popProto.RestrictToAreas)
                        regionAreas.Add(areaRef);
                    regionCell = AssetsToList(popProto.RestrictToCells);
                }

                Picker<PopulationRequiredObjectPrototype> popPicker = new(Game.Random);

                foreach (var popObject in popProto.PopulationObjects)
                    popPicker.Add(popObject);

                if (popPicker.Empty() == false)
                {
                    while (popPicker.PickRemove(out var popObject))
                    {
                        int count = popObject.Count;
                        count = 1; // Fix 
                        var objectProto = popObject.GetPopObject();
                        AddPopulationMarker(objectProto.UsePopulationMarker, objectProto, count, regionAreas, regionCell, PrototypeId.Invalid);
                    }
                }
            }
        }
    }
}
