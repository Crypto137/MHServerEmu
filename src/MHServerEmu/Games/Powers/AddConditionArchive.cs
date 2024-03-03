using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Powers
{
    public class AddConditionArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public Condition Condition { get; set; }

        public AddConditionArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            Condition = new(stream);
        }

        public AddConditionArchive() { }

        public AddConditionArchive(ulong entityId, ulong id, ConditionSerializationFlags serializationFlags, PrototypeId prototypeId, int startTime)
        {
            ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy;
            EntityId = entityId;

            Condition = new()
            {
                Id = id,
                SerializationFlags = serializationFlags,
                CreatorPowerPrototypeId = prototypeId,
                StartTime = startTime
            };
        }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32((uint)ReplicationPolicy);
                cos.WriteRawVarint64(EntityId);
                Condition.Encode(cos);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"Condition: {Condition}");

            return sb.ToString();
        }
    }
}
