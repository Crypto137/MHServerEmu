using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Powers
{
    public class AddConditionArchive
    {
        public uint ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public Condition Condition { get; set; }

        public AddConditionArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            Condition = new(stream);
        }

        public AddConditionArchive() { }

        public AddConditionArchive(ulong entityId, ulong id, uint flags, ulong prototypeId, int startTime)
        {
            ReplicationPolicy = 239;
            EntityId = entityId;
            
            Condition = new();
            Condition.Id = id;
            Condition.Flags = flags.ToBoolArray(16);
            Condition.CreatorPowerPrototypeId = prototypeId;
            Condition.StartTime = startTime;
            Condition.PropertyCollection = new(0);
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawBytes(Condition.Encode());

                cos.Flush();
                return ms.ToArray();
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
