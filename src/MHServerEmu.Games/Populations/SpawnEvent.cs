using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
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
        public SpawnMissionScheduler MissionScheduler;
        public Dictionary<PrototypeId, SpawnScheduler> SpawnMarkerSchedulers;
        public Dictionary<SpawnLocation, SpawnScheduler> SpawnLocationSchedulers;
        public bool RespawnObject;
        public int RespawnDelayMS;

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

        public void Destroy()
        {
            PopulationManager.RemoveSpawnEvent(this);
            PopulationManager.DeScheduleSpawnEvent(this);
            foreach (var spawnGroup in SpawnGroups)
                PopulationManager.RemoveSpawnGroup(spawnGroup);
            SpawnGroups.Clear();
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
                if (spawnScheduler.Any) return false;
            foreach (var spawnScheduler in SpawnLocationSchedulers.Values)
                if (spawnScheduler.Any) return false;
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
            SpawnLocation spawnLocation, PrototypeId missionRef, bool spawnCleanup = false, TimeSpan time = default, 
            bool removeOnSpawnFail = false, bool isMissionMarker = false)
        {
            var random = Game.Random;
            PropertyCollection properties = null;
            if (missionRef != PrototypeId.Invalid)
            {
                properties = new PropertyCollection();
                properties[PropertyEnum.MissionPrototype] = missionRef;

                // HardFix for CH04TR05Barber
                if (missionRef == (PrototypeId)17490825540593458750
                    && populationMarkerRef == (PrototypeId)4913246059230140017) // MissionSpawnedChestMarker
                    populationMarkerRef = (PrototypeId)17169140681234780994; // DeadEndChestMarker
            }

            if (time == default) 
                time = TimeSpan.Zero;
            else 
                time = Game.CurrentTime + time;

            PopulationObject populationObject = new()
            {
                SpawnEvent = this,
                IsMarker = populationMarkerRef != PrototypeId.Invalid,
                IsMissionMarker = isMissionMarker,
                MarkerRef = populationMarkerRef,
                MissionRef = missionRef,
                Random = random,
                Critical = critical,
                Time = time,
                Properties = properties,
                SpawnFlags = SpawnFlags.IgnoreSimulated,
                Object = population,
                SpawnLocation = spawnLocation,
                SpawnCleanup = spawnCleanup,
                RemoveOnSpawnFail = removeOnSpawnFail
            };

            if (PopulationManager.DebugMarker(populationMarkerRef)) Logger.Info($"AddPopulationObject [{critical}] {populationObject}");

            populationObject.Scheduler = AddToScheduler(populationObject);
            return populationObject;
        }

        public SpawnScheduler AddToScheduler(PopulationObject populationObject)
        {
            SpawnScheduler scheduler;
            if (populationObject.IsMarker)
            {
                var markerRef = populationObject.MarkerRef;

                if (populationObject.IsMissionMarker)
                {
                    MissionScheduler ??= new SpawnMissionScheduler(this, populationObject.Critical);
                    scheduler = MissionScheduler;
                }
                else
                {
                    if (SpawnMarkerSchedulers.TryGetValue(markerRef, out var markerScheduler))
                    {
                        scheduler = markerScheduler;
                    }
                    else
                    {
                        scheduler = new(this);
                        SpawnMarkerSchedulers[markerRef] = scheduler;                      
                    }
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

        public void PopulationRegisty(PopulationPrototype populationProto)
        {
            int objCount = 0;
            int markerCount = 0;
            var manager = PopulationManager;
            float spawnableNavArea = Area.SpawnableNavArea;
            if (spawnableNavArea <= 0.0f || populationProto.UseSpawnMap) return; // SpawnMap
            if (populationProto.Themes == null || populationProto.Themes.List.IsNullOrEmpty()) return;

            var spawnLocation = new SpawnLocation(Region, Area);

            float density = spawnableNavArea / PopulationPrototype.PopulationClusterSq * (populationProto.ClusterDensityPct / 100.0f);
            var themeProto = GameDatabase.GetPrototype<PopulationThemePrototype>(populationProto.Themes.List[0].Object);
            var picker = PopulationObject.PopulatePicker(manager.Random, themeProto.Enemies.List);
            while (density > 0.0f && picker.Pick(out var objectProto))
            {
                density -= objectProto.GetAverageSize();
                AddPopulationObject(PrototypeId.Invalid, objectProto, false, spawnLocation, PrototypeId.Invalid, true);
                objCount++;
            }

            List<PopulationObjectInstancePrototype> encounters = new();
            if (populationProto.GlobalEncounters != null) PopulationObject.GetContainedEncounters(populationProto.GlobalEncounters.List, encounters);
            if (themeProto.Encounters != null) PopulationObject.GetContainedEncounters(themeProto.Encounters.List, encounters);

            var registry = Region.SpawnMarkerRegistry;
            var random = Game.Random;
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
                    int slots = registry.CalcMarkerReservations(markerRef, Area.PrototypeDataRef);

                    if (slots == 0)
                        spawnPicker = new(null, 0);
                    else
                    {
                        density = populationProto.GetEncounterDensity(markerRef) / 100.0f;
                        int count = Math.Max(1, (int)(slots * density));
                        spawnPicker = new(new(random), count);
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
                    AddPopulationObject(markerRef, objectProto, false, spawnLocation, PrototypeId.Invalid, true);
                markerCount++;
            }
            // Logger.Debug($"Population [{populationProto.SpawnMapDensityMin}][{GameDatabase.GetFormattedPrototypeName(populationProto.DataRef)}][{objCount}][{markerCount}]");
        }

        public PopulationObject AddHeatObject(Vector3 position, PopulationObjectPrototype population, SpawnHeat spawnHeat)
        {
            var random = Game.Random;
            PropertyCollection properties = null;

            var cell = Area.GetCellAtPosition(position);
            var spawnLocation = new SpawnLocation(Region, cell);

            PopulationObject populationObject = new()
            {
                SpawnEvent = this,
                IsMarker = false,
                MarkerRef = PrototypeId.Invalid,
                MissionRef = PrototypeId.Invalid,
                Position = position,
                Random = random,
                Critical = false,
                Time = TimeSpan.Zero,
                Properties = properties,
                SpawnFlags = SpawnFlags.IgnoreSimulated,
                Object = population,
                SpawnLocation = spawnLocation,
                SpawnHeat = spawnHeat,
                SpawnCleanup = true,
                RemoveOnSpawnFail = true
            };

            populationObject.Scheduler = AddToScheduler(populationObject);
            return populationObject;
        }
    }

    public class MissionSpawnEvent : SpawnEvent
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
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
            bool notOpen = missionProto is not OpenMissionPrototype;
            bool spawnCleanup = notOpen;
            bool critical = notOpen || missionProto.PopulationRequired;
            var difficultyRef = Region.DifficultyTierRef;

            var time = TimeSpan.Zero;

            // Fix conflict CH04TRChestController vs CH04TR02HauntedWarehouse
            if (missionProto.DataRef == (PrototypeId)937322365109346758) time = TimeSpan.FromSeconds(1);

            if (missionProto.PopulationSpawns.HasValue())            
                foreach (var entry in missionProto.PopulationSpawns)
                {
                    if (entry.AllowedInDifficulty(difficultyRef) == false) continue;
                    if (entry.FilterRegion(Region.Prototype) == false) continue;
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

                    bool isMissionMarker = entry.Population.UsePopulationMarker != PrototypeId.Invalid;

                    var spawnLocation = new SpawnLocation(Region, entry.RestrictToAreas, entry.RestrictToCells);
                    for (var i = 0; i < entry.Count; i++)                        
                        AddPopulationObject(entry.Population.UsePopulationMarker, entry.Population, critical, spawnLocation, 
                            missionProto.DataRef, spawnCleanup, time, false, isMissionMarker);
                }
        }

        public override void OnSpawnedPopulation()
        {
            /*if (MissionRef == (PrototypeId)8848708389702214357) // Debug mission
            {
                string str = "";
                foreach (var scheduler in MissionScheduler.SpawnMissionObjects.Values)
                    str += $"{scheduler.MissionObjects.Count} ";
                Logger.Warn($"OnSpawnedPopulation {MissionRef.GetNameFormatted()} [{str}]");
            }*/

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
        public PrototypeId ContextRef;

        public MetaStateSpawnEvent(PrototypeId contextRef, Region region) : base(region)
        {
            ContextRef = contextRef;
        }

        public void AddRequiredObjects(PopulationRequiredObjectPrototype[] populationObjects, SpawnLocation spawnLocation, PrototypeId missionRef, 
            bool spawnCleanup, bool removeOnSpawnFail, TimeSpan time = default)
        {
            float spawnableArea = spawnLocation.CalcSpawnableArea();
            var difficultyRef = Region.DifficultyTierRef;

            foreach (var reqObject in populationObjects)
            {
                if (reqObject.AllowedInDifficulty(difficultyRef) == false) continue;
                int count = reqObject.Count;
                var objectProto = reqObject.GetPopObject();
                if (count <= 0 && reqObject.Density > 0.0f)
                {                        
                    float averageSize = objectProto.GetAverageSize();
                    count = (int)(reqObject.Density / averageSize * (spawnableArea / PopulationPrototype.PopulationClusterSq));
                }                

                var spawnLocationReq = new SpawnLocation(spawnLocation, reqObject.RestrictToAreas, reqObject.RestrictToCells);

                for (int i = 0; i < count; i++)
                {
                    AddPopulationObject(objectProto.UsePopulationMarker, objectProto, reqObject.Critical, spawnLocationReq,
                        missionRef, spawnCleanup, time, removeOnSpawnFail);
                }
            }
        }
    }
}
