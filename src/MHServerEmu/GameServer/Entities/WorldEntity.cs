using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class WorldEntity : Entity
    {
        public EntityTrackingContextMap[] TrackingContextMap { get; set; }
        public Condition[] ConditionCollection { get; set; }
        public PowerCollectionRecord[] PowerCollection { get; set; }
        public int UnkEvent { get; set; }

        public WorldEntity(EntityBaseData baseData, byte[] archiveData) : base(baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadEntityFields(stream);
            ReadWorldEntityFields(stream);
            ReadUnknownFields(stream);
        }

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

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteEntityFields(cos);
                WriteWorldEntityFields(cos);
                WriteUnknownFields(cos);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);
            WriteWorldEntityString(sb);
            WriteUnknownFieldString(sb);
            return sb.ToString();
        }

        protected void ReadWorldEntityFields(CodedInputStream stream)
        {
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

        protected void WriteWorldEntityFields(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)TrackingContextMap.Length);
            foreach (EntityTrackingContextMap entry in TrackingContextMap)
                stream.WriteRawBytes(entry.Encode());

            stream.WriteRawVarint64((ulong)ConditionCollection.Length);
            foreach (Condition condition in ConditionCollection)
                stream.WriteRawBytes(condition.Encode());

            if ((ReplicationPolicy & 0x1) > 0)
            {
                stream.WriteRawVarint32((uint)PowerCollection.Length);
                for (int i = 0; i < PowerCollection.Length; i++)
                    stream.WriteRawBytes(PowerCollection[i].Encode());
            }

            stream.WriteRawInt32(UnkEvent);
        }

        protected void WriteWorldEntityString(StringBuilder sb)
        {
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
