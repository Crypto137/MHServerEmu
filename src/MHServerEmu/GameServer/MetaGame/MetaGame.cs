using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.MetaGame
{
    public class MetaGame : Entity
    {
        public ReplicatedString Name { get; set; }

        public MetaGame(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public MetaGame(EntityBaseData baseData) : base(baseData) { }

        public MetaGame(EntityBaseData baseData, uint replicationPolicy, ReplicatedPropertyCollection propertyCollection,
            ReplicatedString name) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;

            Name = name;
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
