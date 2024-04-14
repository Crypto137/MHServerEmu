using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.MetaGames
{
    public class MetaGame : Entity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public ReplicatedVariable<string> Name { get; set; }

        // new
        public MetaGame(Game game) : base(game) { }
        public override void Initialize(EntitySettings settings)
        {
            base.Initialize(settings);
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            Name = new(0, "");
            Region region = Game.RegionManager.GetRegion(settings.RegionId);
            region.RegisterMetaGame(this);
        }

        // old 
        public MetaGame(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public MetaGame(EntityBaseData baseData) : base(baseData) { }

        public MetaGame(EntityBaseData baseData, AOINetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection properties,
            ReplicatedVariable<string> name) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            Properties = properties;

            Name = name;
        }

        public override void Destroy()
        {
            // TODO clear Teams;
            base.Destroy();
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            Name = new(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            Name.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"Name: {Name}");
        }

        // TODO event registry States
        public void RegistyStates()
        {
            Region region = Game.RegionManager.GetRegion(RegionId);           
            if (region == null) return;
            var popManager = region.PopulationManager;
            if (EntityPrototype is not MetaGamePrototype metaGameProto) return;
            if (metaGameProto.GameModes.HasValue())
            {
                var gameMode = metaGameProto.GameModes.First().As<MetaGameModePrototype>();
                if (gameMode == null) return;

                if (gameMode.ApplyStates.HasValue())
                    foreach(var state in gameMode.ApplyStates)
                        popManager.MetaStateRegisty(state);

                if (region.PrototypeId == RegionPrototypeId.HoloSimARegion1to60) // Hardcode for Holo-Sim
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    int wave = Game.Random.Next(0, stateMode.States.Length);
                    popManager.MetaStateRegisty(stateMode.States[wave]);
                } 
                else if (region.PrototypeId == RegionPrototypeId.LimboRegionL60) // Hardcode for Limbo
                {
                    MetaGameStateModePrototype stateMode = gameMode as MetaGameStateModePrototype;
                    popManager.MetaStateRegisty(stateMode.States[0]);
                }
                else if (region.PrototypeId == RegionPrototypeId.CH0402UpperEastRegion) // Hack for Moloids
                    popManager.MetaStateRegisty((PrototypeId)7730041682554854878); // CH04UpperMoloids
            }
        }
    }
}
