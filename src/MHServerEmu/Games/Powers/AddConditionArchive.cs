using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Powers
{
    public class AddConditionArchive
    {
        public AoiNetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public Condition Condition { get; set; }

        public AddConditionArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AoiNetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            Condition = new(stream);
        }

        public AddConditionArchive() { }

        public AddConditionArchive(ulong entityId, ulong id, uint flags, PrototypeId prototypeId, int startTime)
        {
            ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel0 | AoiNetworkPolicyValues.AoiChannel1 | AoiNetworkPolicyValues.AoiChannel2
                | AoiNetworkPolicyValues.AoiChannel3 |  AoiNetworkPolicyValues.AoiChannel5 | AoiNetworkPolicyValues.AoiChannelClientOnly
                | AoiNetworkPolicyValues.AoiChannel7;
            EntityId = entityId;

            Condition = new()
            {
                Id = id,
                Flags = flags.ToBoolArray(16),
                CreatorPowerPrototypeId = prototypeId,
                StartTime = startTime,
                PropertyCollection = new(0)
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
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"Condition: {Condition}");

            return sb.ToString();
        }
    }
}
