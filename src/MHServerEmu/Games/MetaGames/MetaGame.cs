using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

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
    }
}
