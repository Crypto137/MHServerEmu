using System.Text;

namespace MHServerEmu.Games.Missions
{
    public readonly struct InteractionTag
    {
        // Relevant protobuf: NetStructMissionInteractionTag

        public ulong EntityId { get; }
        public ulong RegionId { get; }
        //public TimeSpan Timestamp { get; }     // GameTime, used only in non-replication archives, tags older than 1 day are discarded during deserialization

        public InteractionTag(ulong entityId, ulong regionId)
        {
            EntityId = entityId;
            RegionId = regionId;
            //Timestamp = TimeSpan.Zero;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(EntityId)}: {EntityId}");
            sb.AppendLine($"{nameof(RegionId)}: {RegionId}");
            //sb.AppendLine($"{nameof(Timestamp)}: {Clock.GameTimeToDateTime(Timestamp)}");
            return sb.ToString();
        }
    }
}
