using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    public class SpawnEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Region Region;
        public Game Game;
        public PopulationManager PopulationManager;
        public HashSet<ulong> SpawnGroups;
        public HashSet<ulong> SpawnedEntities;
        public Dictionary<PrototypeId, SpawnScheduler> SpawnMarkerSchedulers;
        public Dictionary<SpawnLocation, SpawnScheduler> SpawnLocationSchedulers;

        public SpawnEvent(Region region)
        {
            Region = region;
            Game = region.Game;
            PopulationManager = region.PopulationManager;
            PopulationManager.AddSpawnEvent(this);
            SpawnGroups = new();
            SpawnedEntities = new();
            SpawnMarkerSchedulers = new();
            SpawnLocationSchedulers = new();
        }

        public void SetSpawnData(ulong groupId, List<WorldEntity> entities)
        {
            var group = PopulationManager.GetSpawnGroup(groupId);
            if (group != null) group.SpawnEvent = this;
            SpawnGroups.Add(groupId);
            foreach (var entity in entities)
                SpawnedEntities.Add(entity.Id);
        }

        public bool IsSpawned()
        {
            foreach(var spawnScheduler in SpawnMarkerSchedulers.Values)
                if (spawnScheduler.ScheduledObjects.Count > 0) return false;
            foreach (var spawnScheduler in SpawnLocationSchedulers.Values)
                if (spawnScheduler.ScheduledObjects.Count > 0) return false;
            return true;
        }

        public void Respawn()
        {
            foreach(var groupId in SpawnGroups) 
            {
                var group = PopulationManager.GetSpawnGroup(groupId);
                group?.Respawn();
            }
        }

        public PopulationObject AddPopulationObject(PrototypeId populationMarkerRef, PopulationObjectPrototype population, bool critical,
            SpawnLocation spawnLocation, PrototypeId missionRef, TimeSpan time = default)
        {
            /*HashSet<PrototypeId> entities = new();
            population.GetContainedEntities(entities);
            if (entities.Count > 0) 
                Logger.Debug($"SpawnMarker[{GameDatabase.GetFormattedPrototypeName(entities.First())}][{population.IgnoreBlackout}] {GameDatabase.GetFormattedPrototypeName(populationMarkerRef)}");*/
            GRandom random = Game.Random;
            PropertyCollection properties = null;
            if (missionRef != PrototypeId.Invalid)
            {
                properties = new PropertyCollection();
                properties[PropertyEnum.MissionPrototype] = missionRef;
            }
            if (time == default) time = TimeSpan.Zero;
            PopulationObject populationObject = new()
            {
                SpawnEvent = this,
                IsMarker = populationMarkerRef != PrototypeId.Invalid,
                MarkerRef = populationMarkerRef,
                MissionRef = missionRef,
                Random = random,
                Critical = critical,
                Time = time,
                Properties = properties,
                SpawnFlags = SpawnFlags.IgnoreSimulated,
                Object = population,
                SpawnLocation = spawnLocation,
            };

            populationObject.Scheduler = AddToScheduler(populationObject);
            return populationObject;
        }

        private SpawnScheduler AddToScheduler(PopulationObject populationObject)
        {
            SpawnScheduler scheduler;
            if (populationObject.IsMarker)
            {
                if (SpawnMarkerSchedulers.TryGetValue(populationObject.MarkerRef, out var markerScheduler))
                {
                    scheduler = markerScheduler;
                }
                else
                {
                    scheduler = new(this);
                    SpawnMarkerSchedulers[populationObject.MarkerRef] = scheduler;
                }
            }
            else
            {
                if (SpawnLocationSchedulers.TryGetValue(populationObject.SpawnLocation, out var locationScheduler))
                {
                    scheduler = locationScheduler;
                }
                else
                {
                    scheduler = new(this);
                    SpawnLocationSchedulers[populationObject.SpawnLocation] = scheduler;
                }
            }
           
            scheduler.Push(populationObject);
            return scheduler;
        }

        public void Schedule()
        {
            PopulationManager.ScheduleSpawnEvent(this);
        }

        public virtual void OnSpawnedPopulation() { }
        public virtual void OnUpdateSimulation() { }
    }

    public class PopulationAreaSpawnEvent : SpawnEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Area Area;

        public PopulationAreaSpawnEvent(Area area, Region region) : base (region)
        {
            Area = area;
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

        public void PopulationRegisty(PopulationPrototype populationProto)
        {
            int objCount = 0;
            int markerCount = 0;
            var manager = PopulationManager;
            float spawnableNavArea = Area.SpawnableNavArea;
            //if (populationProto.SpawnMapEnabled || (populationProto.SpawnMapDensityMin > 0.0 && populationProto.SpawnMapDensityMax > 0.0f)) return;
            if (populationProto.Themes == null || populationProto.Themes.List.IsNullOrEmpty()) return;

            var spawnLocation = new SpawnLocation(Region, Area);

            float density = spawnableNavArea / PopulationPrototype.PopulationClusterSq * (populationProto.ClusterDensityPct / 100.0f);
            var themeProto = GameDatabase.GetPrototype<PopulationThemePrototype>(populationProto.Themes.List[0].Object);
            var picker = PopulatePicker(manager.Random, themeProto.Enemies.List);
            while (density > 0.0f && picker.Pick(out var objectProto))
            {
                density -= objectProto.GetAverageSize();
                AddPopulationObject(PrototypeId.Invalid, objectProto, false, spawnLocation, PrototypeId.Invalid);
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
                for (int i = 0; i < spawnPicker.Count; i++)
                    AddPopulationObject(markerRef, objectProto, false, spawnLocation, PrototypeId.Invalid);
                markerCount++;
            }
            Logger.Debug($"Population [{populationProto.SpawnMapDensityMin}][{GameDatabase.GetFormattedPrototypeName(populationProto.DataRef)}][{objCount}][{markerCount}]");
        }
    }

    public class MissionSpawnEvent : SpawnEvent
    {
        public PrototypeId MissionRef;
        public MissionManager MissionManager;

        public MissionSpawnEvent(PrototypeId missionRef, MissionManager missionManager, Region region) : base(region) 
        {
            MissionRef = missionRef;
            MissionManager = missionManager;
        }

        public void MissionRegistry(MissionPrototype missionProto)
        {
            if (missionProto == null) return;
            bool critical = missionProto is not OpenMissionPrototype || missionProto.PopulationRequired;

            if (missionProto.PopulationSpawns.HasValue())            
                foreach (var entry in missionProto.PopulationSpawns)
                {
                    if (entry.RestrictToAreas.HasValue()) // check areas
                    {
                        bool foundArea = false;
                        foreach (var areaRef in entry.RestrictToAreas)
                            if (Region.GetArea(areaRef) != null)
                            {
                                foundArea = true;
                                break;
                            }

                        if (foundArea == false) continue;
                    }

                    var spawnLocation = new SpawnLocation(Region, entry.RestrictToAreas, entry.RestrictToCells);
                    for (var i = 0; i < entry.Count; i++)                        
                        AddPopulationObject(entry.Population.UsePopulationMarker, entry.Population, critical, spawnLocation, missionProto.DataRef);
                }
        }
        public override void OnSpawnedPopulation()
        {
            if (MissionManager.IsRegionMissionManager() && IsSpawned()) 
                MissionManager.OnSpawnedPopulation(MissionRef);
        }

        public override void OnUpdateSimulation()
        {
            var mission = MissionManager.FindMissionByDataRef(MissionRef);
            mission?.OnUpdateSimulation(this);
        }
    }
    
    public class MetaStateSpawnEvent : SpawnEvent
    {
        public MetaGame MetaGame;

        public MetaStateSpawnEvent(MetaGame metaGame, Region region) : base(region)
        {
            MetaGame = metaGame;
        }

        public void AddRequiredObjects(PopulationRequiredObjectPrototype[] populationObjects, SpawnLocation spawnLocation)
        {
            Picker<PopulationRequiredObjectPrototype> popPicker = new(Game.Random);
            float spawnableArea = spawnLocation.CalcSpawnableArea();

            foreach (var reqObject in populationObjects)
                popPicker.Add(reqObject);

            if (popPicker.Empty() == false)
                while (popPicker.PickRemove(out var reqObject))
                {
                    int count = reqObject.Count;
                    var objectProto = reqObject.GetPopObject();
                    if (reqObject.Density > 0.0f)
                    {                        
                        float averageSize = objectProto.GetAverageSize();
                        count = (int)(reqObject.Density / averageSize * (spawnableArea / PopulationPrototype.PopulationClusterSq));
                    }
                    if (RegionHelper.TEMP_IsPatrolRegion(Region.PrototypeDataRef)) count = 1;

                    var spawnLocationReq = new SpawnLocation(spawnLocation, reqObject.RestrictToAreas, reqObject.RestrictToCells);

                    for (int i = 0; i < count; i++)
                        AddPopulationObject(objectProto.UsePopulationMarker, objectProto, reqObject.Critical, spawnLocationReq, PrototypeId.Invalid);
                }
        }
    }
}
