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
            }
        }
    }
}
