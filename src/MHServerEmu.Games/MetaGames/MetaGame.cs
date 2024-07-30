using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Populations;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGame : Entity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        protected ReplicatedVariable<string> _name = new(0, string.Empty);
        public Region Region { get; private set; }

        private Dictionary<PrototypeId, MetaStateSpawnEvent> _metaStateSpawnMap = new();
        // new
        public MetaGame(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            _name = new(0, "");
            Region = Game.RegionManager.GetRegion(settings.RegionId);
            Region?.RegisterMetaGame(this);

            return true;
        }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);
            // if (archive.IsTransient)
            success &= Serializer.Transfer(archive, ref _name);
            return success;
        }

        public override void Destroy()
        {
            // TODO clear Teams;
            base.Destroy();
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_name)}: {_name}");
        }

        public MetaStateSpawnEvent GetMetaStateEvent(PrototypeId state)
        {
            if (_metaStateSpawnMap.TryGetValue(state, out var spawnEvent))
            {
                spawnEvent = _metaStateSpawnMap[state];
            }
            else
            {
                spawnEvent = new MetaStateSpawnEvent(this, Game.RegionManager.GetRegion(RegionId));
                _metaStateSpawnMap[state] = spawnEvent;
            }
            return spawnEvent;
        }

        public void MetaStateRegisty(PrototypeId stateRef)
        {
            var metastate = GameDatabase.GetPrototype<MetaStatePrototype>(stateRef);

            if (metastate is MetaStateMissionProgressionPrototype missionProgression)
            {
                if (missionProgression.StatesProgression.HasValue())
                    MetaStateRegisty(missionProgression.StatesProgression.First());
            }
            else if (metastate is MetaStateMissionActivatePrototype missionActivate)
            {
                if (missionActivate.SubStates.HasValue())
                    foreach (var state in missionActivate.SubStates)
                        MetaStateRegisty(state);

                var metaStateRef = missionActivate.DataRef;
                Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{missionActivate.PopulationObjects.Length}]");
                var metaStateEvent = GetMetaStateEvent(metaStateRef);
                var spawnLocation = new SpawnLocation(Region, missionActivate.PopulationAreaRestriction, null);
                metaStateEvent.AddRequiredObjects(missionActivate.PopulationObjects, spawnLocation);
                metaStateEvent.Schedule();
            }
            else if (metastate is MetaStateMissionSequencerPrototype missionSequencer)
            {
                if (missionSequencer.Sequence.HasValue())
                    foreach (var missionEntry in missionSequencer.Sequence)
                    {
                        var metaStateRef = metastate.DataRef;
                        Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{missionEntry.PopulationObjects.Length}]");
                        var metaStateEvent = GetMetaStateEvent(metaStateRef);
                        var spawnLocation = new SpawnLocation(Region, missionEntry.PopulationAreaRestriction, null);
                        metaStateEvent.AddRequiredObjects(missionEntry.PopulationObjects, spawnLocation);
                        metaStateEvent.Schedule();
                    }
            }
            else if (metastate is MetaStateWaveInstancePrototype waveInstance)
            {
                if (waveInstance.States.HasValue())
                    foreach (var state in waveInstance.States)
                        MetaStateRegisty(state);
            }
            else if (metastate is MetaStatePopulationMaintainPrototype popProto && popProto.PopulationObjects.HasValue())
            {
                var metaStateRef = popProto.DataRef;
                Logger.Debug($"State [{GameDatabase.GetFormattedPrototypeName(metaStateRef)}][{popProto.PopulationObjects.Length}]");
                var areas = popProto.RestrictToAreas;
                if (popProto.DataRef == (PrototypeId)7730041682554854878 && (RegionPrototypeId)RegionId == RegionPrototypeId.CH0402UpperEastRegion) areas = null; // Hack for Moloids
                var metaStateEvent = GetMetaStateEvent(metaStateRef);
                var spawnLocation = new SpawnLocation(Region, areas, popProto.RestrictToCells);
                metaStateEvent.AddRequiredObjects(popProto.PopulationObjects, spawnLocation);
                metaStateEvent.Schedule();
            }
        }

        // TODO event registry States
        public void RegistyStates()
        {
            Region region = Game.RegionManager.GetRegion(RegionId);           
            if (region == null) return;
            if (Prototype is not MetaGamePrototype metaGameProto) return;
            if (metaGameProto.GameModes.HasValue())
            {
                var gameMode = metaGameProto.GameModes.First().As<MetaGameModePrototype>();
                if (gameMode == null) return;

                if (gameMode.ApplyStates.HasValue())
                    foreach(var state in gameMode.ApplyStates)
                        MetaStateRegisty(state);

                if (region.PrototypeId == RegionPrototypeId.HoloSimARegion1to60) // Hardcode for Holo-Sim
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    int wave = Game.Random.Next(0, stateMode.States.Length);
                    MetaStateRegisty(stateMode.States[wave]);
                } 
                else if (region.PrototypeId == RegionPrototypeId.LimboRegionL60) // Hardcode for Limbo
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    MetaStateRegisty(stateMode.States[0]);
                }
                else if (region.PrototypeId == RegionPrototypeId.CH0402UpperEastRegion) // Hack for Moloids
                    MetaStateRegisty((PrototypeId)7730041682554854878); // CH04UpperMoloids
                else if (region.PrototypeId == RegionPrototypeId.SurturRaidRegionGreen) // Hardcode for Surtur
                {   
                    var stateRef = (PrototypeId)5463286934959496963; // SurturMissionProgressionStateFiveMan
                    var missionProgression = stateRef.As<MetaStateMissionProgressionPrototype>();
                    foreach(var state in missionProgression.StatesProgression)
                        MetaStateRegisty(state);
                }
            }
        }
    }
}
