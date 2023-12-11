using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class WorldEntity : Entity
    {
        public EntityTrackingContextMap[] TrackingContextMap { get; set; }
        public Condition[] ConditionCollection { get; set; }
        public PowerCollectionRecord[] PowerCollection { get; set; }
        public int UnkEvent { get; set; }


        private RegionLocation _location; // TODO init;
        public Cell Cell { get => _location.Cell; }

        public WorldEntity(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public WorldEntity(EntityBaseData baseData) : base(baseData) { }

        public WorldEntity(EntityBaseData baseData, uint replicationPolicy, ulong replicationId) : base(baseData)
        {
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = new(replicationId);
            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;
        }

        public WorldEntity(EntityBaseData baseData, ulong replicationId, Vector3 mapPosition, int health, int mapAreaId,
            int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef) : base(baseData)
        {
            ReplicationPolicy = 0x20;

            PropertyCollection = new(replicationId, new()
            {
                new(PropertyEnum.MapPosition, mapPosition),
                new(PropertyEnum.Health, health),
                new(PropertyEnum.MapAreaId, mapAreaId),
                new(PropertyEnum.HealthMaxOther, healthMaxOther),
                new(PropertyEnum.MapRegionId, mapRegionId),
                new(PropertyEnum.MapCellId, mapCellId),
                new(PropertyEnum.ContextAreaRef, contextAreaRef)
            });

            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;
        }

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            TrackingContextMap = new EntityTrackingContextMap[stream.ReadRawVarint64()];
            for (int i = 0; i < TrackingContextMap.Length; i++)
                TrackingContextMap[i] = new(stream);

            ConditionCollection = new Condition[stream.ReadRawVarint64()];
            for (int i = 0; i < ConditionCollection.Length; i++)
                ConditionCollection[i] = new(stream);

            // Gazillion::PowerCollection::SerializeRecordCount
            if ((ReplicationPolicy & 0x1) > 0)
            {
                PowerCollection = new PowerCollectionRecord[stream.ReadRawVarint32()];
                if (PowerCollection.Length > 0)
                {
                    // Records after the first one may require the previous record to get values from
                    PowerCollection[0] = new(stream, null);
                    for (int i = 1; i < PowerCollection.Length; i++)
                        PowerCollection[i] = new(stream, PowerCollection[i - 1]);
                }
            }
            else
            {
                PowerCollection = Array.Empty<PowerCollectionRecord>();
            }

            UnkEvent = stream.ReadRawInt32();
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawVarint64((ulong)TrackingContextMap.Length);
            foreach (EntityTrackingContextMap entry in TrackingContextMap) entry.Encode(stream);

            stream.WriteRawVarint64((ulong)ConditionCollection.Length);
            foreach (Condition condition in ConditionCollection) condition.Encode(stream);

            if ((ReplicationPolicy & 0x1) > 0)
            {
                stream.WriteRawVarint32((uint)PowerCollection.Length);
                for (int i = 0; i < PowerCollection.Length; i++) PowerCollection[i].Encode(stream);
            }

            stream.WriteRawInt32(UnkEvent);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < TrackingContextMap.Length; i++)
                sb.AppendLine($"TrackingContextMap{i}: {TrackingContextMap[i]}");

            for (int i = 0; i < ConditionCollection.Length; i++)
                sb.AppendLine($"ConditionCollection{i}: {ConditionCollection[i]}");

            for (int i = 0; i < PowerCollection.Length; i++)
                sb.AppendLine($"PowerCollection{i}: {PowerCollection[i]}");

            sb.AppendLine($"UnkEvent: {UnkEvent}");
        }
    }
}
