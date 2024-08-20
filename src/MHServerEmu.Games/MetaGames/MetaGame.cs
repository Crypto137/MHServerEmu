using System.Text;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGame : Entity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        protected RepString _name;
        protected ulong _regionId;

        public MetaGame(Game game) : base(game) { }

        public override bool Initialize(EntitySettings settings)
        {
            base.Initialize(settings);

            //_name = new(0, "");
            _regionId = settings.RegionId;

            Region region = Game.RegionManager.GetRegion(_regionId);
            if (region != null)
            {
                region.RegisterMetaGame(this);
                // TODO: Other stuff
            }
            else
            {
                Logger.Warn("Initialize(): region == null");
            }

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

        public Region GetRegion()
        {
            if (_regionId == 0)
                return null;

            return Game.RegionManager.GetRegion(_regionId);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_name)}: {_name}");
        }

        // TODO event registry States
        public void RegisterStates()
        {
            Region region = Game.RegionManager.GetRegion(_regionId);           
            if (region == null) return;

            PopulationManager popManager = region.PopulationManager;
            
            if (Prototype is not MetaGamePrototype metaGameProto) return;
            
            if (metaGameProto.GameModes.HasValue())
            {
                var gameMode = metaGameProto.GameModes.First().As<MetaGameModePrototype>();
                if (gameMode == null) return;

                if (gameMode.ApplyStates.HasValue())
                {
                    foreach (PrototypeId state in gameMode.ApplyStates)
                        popManager.RegisterMetaState(state);
                }

                if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.HoloSimARegion1to60) // Hardcode for Holo-Sim
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    int wave = Game.Random.Next(0, stateMode.States.Length);
                    popManager.RegisterMetaState(stateMode.States[wave]);
                } 
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.LimboRegionL60) // Hardcode for Limbo
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    popManager.RegisterMetaState(stateMode.States[0]);
                }
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.CH0402UpperEastRegion) // Hack for Moloids
                {
                    popManager.RegisterMetaState((PrototypeId)7730041682554854878); // CH04UpperMoloids
                }
                else if (region.PrototypeDataRef == (PrototypeId)RegionPrototypeId.SurturRaidRegionGreen) // Hardcode for Surtur
                {   
                    var stateRef = (PrototypeId)5463286934959496963; // SurturMissionProgressionStateFiveMan
                    var missionProgression = stateRef.As<MetaStateMissionProgressionPrototype>();
                    foreach(PrototypeId state in missionProgression.StatesProgression)
                        popManager.RegisterMetaState(state);
                }
            }
        }
    }
}
