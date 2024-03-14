using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Missions
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(EntityId);
            stream.WriteRawVarint64(RegionId);
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
