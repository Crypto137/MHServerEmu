using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Missions
{
    public class InteractionTag
    {
        public ulong EntityId { get; set; }
        public ulong RegionId { get; set; }
        public ulong GameTime { get; set; }     // unused

        public InteractionTag(CodedInputStream stream)
        {
            EntityId = stream.ReadRawVarint64();
            RegionId = stream.ReadRawVarint64();
        }

        public InteractionTag(ulong entityId, ulong regionId)
        {
            EntityId = entityId;
            RegionId = regionId;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawVarint64(RegionId);
                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"RegionId: {RegionId}");
            //sb.AppendLine($"GameTime: {GameTime}");
            return sb.ToString();
        }
    }
}
