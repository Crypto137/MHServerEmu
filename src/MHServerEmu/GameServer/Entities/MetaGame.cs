using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class MetaGame : Entity
    {
        public ReplicatedString Name { get; set; }

        public MetaGame(EntityBaseData baseData, byte[] archiveData) : base(baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            DecodeEntityFields(stream);

            Name = new(stream);
        }

        public MetaGame(EntityBaseData baseData) : base(baseData) { }

        public MetaGame(EntityBaseData baseData, uint replicationPolicy, ReplicatedPropertyCollection propertyCollection,
            ReplicatedString name) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;

            Name = name;
        }

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Encode
                EncodeEntityFields(cos);

                cos.WriteRawBytes(Name.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);

            sb.AppendLine($"Name: {Name}");

            return sb.ToString();
        }
    }
}
