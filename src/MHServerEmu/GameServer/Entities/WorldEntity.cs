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
        public PrototypeCollectionEntry[] UnknownPrototypes { get; set; }
        public Condition[] Conditions { get; set; }
        public int UnknownPowerVar { get; set; }

        public WorldEntity(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadEntityFields(stream);
            ReadWorldEntityFields(stream);
            ReadUnknownFields(stream);
        }

        public WorldEntity() { }

        public WorldEntity(ulong replicationId, Vector3 mapPosition, int health, int mapAreaId,
            int healthMaxOther, ulong mapRegionId, int mapCellId, ulong contextAreaRef)
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

            UnknownPrototypes = Array.Empty<PrototypeCollectionEntry>();
            Conditions = Array.Empty<Condition>();
            UnknownPowerVar = 0;
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
            UnknownPrototypes = new PrototypeCollectionEntry[stream.ReadRawVarint64()];
            for (int i = 0; i < UnknownPrototypes.Length; i++)
                UnknownPrototypes[i] = new(stream);

            Conditions = new Condition[stream.ReadRawVarint64()];
            for (int i = 0; i < Conditions.Length; i++)
                Conditions[i] = new(stream);

            // Gazillion::PowerCollection::SerializeRecordCount
            UnknownPowerVar = stream.ReadRawInt32();
        }

        protected void WriteWorldEntityFields(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)UnknownPrototypes.Length);
            foreach (PrototypeCollectionEntry entry in UnknownPrototypes)
                stream.WriteRawBytes(entry.Encode());

            stream.WriteRawVarint64((ulong)Conditions.Length);
            foreach (Condition condition in Conditions)
                stream.WriteRawBytes(condition.Encode());

            stream.WriteRawInt32(UnknownPowerVar);
        }

        protected void WriteWorldEntityString(StringBuilder sb)
        {
            for (int i = 0; i < UnknownPrototypes.Length; i++)
                sb.AppendLine($"UnknownPrototype{i}: {UnknownPrototypes[i]}");

            for (int i = 0; i < Conditions.Length; i++)
                sb.AppendLine($"Condition{i}: {Conditions[i]}");

            sb.AppendLine($"UnknownPowerVar: 0x{UnknownPowerVar:X}");
        }
    }
}
