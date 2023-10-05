using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Powers;

namespace MHServerEmu.GameServer.Entities
{
    public class Transition : WorldEntity
    {
        public string TransitionName { get; set; }
        public Destination[] Destinations { get; set; }

        public Transition(EntityBaseData baseData, byte[] archiveData) : base(baseData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);
            DecodeEntityFields(stream);
            DecodeWorldEntityFields(stream);

            TransitionName = stream.ReadRawString();

            Destinations = new Destination[stream.ReadRawVarint64()];
            for (int i = 0; i < Destinations.Length; i++)
                Destinations[i] = new(stream);
        }

        public Transition(EntityBaseData baseData, EntityTrackingContextMap[] trackingContextMap, Condition[] conditionCollection,
            PowerCollectionRecord[] powerCollection, int unkEvent, 
            string transitionName, Destination[] destinations) : base(baseData)
        {
            TrackingContextMap = trackingContextMap;
            ConditionCollection = conditionCollection;
            PowerCollection = powerCollection;
            UnkEvent = unkEvent;
            TransitionName = transitionName;
            Destinations = destinations;
        }

        public override byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Encode
                EncodeEntityFields(cos);
                EncodeWorldEntityFields(cos);

                cos.WriteRawString(TransitionName);
                cos.WriteRawVarint64((ulong)Destinations.Length);
                foreach (Destination destination in Destinations) cos.WriteRawBytes(destination.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new(); 
            WriteEntityString(sb);
            WriteWorldEntityString(sb);

            sb.AppendLine($"TransitionName: {TransitionName}");
            for (int i = 0; i < Destinations.Length; i++) sb.AppendLine($"Destination{i}: {Destinations[i]}");

            return sb.ToString();
        }
    }

    public class Destination
    {
        public int Type { get; set; }
        public ulong Region { get; set; }
        public ulong Area { get; set; }
        public ulong Cell { get; set; }
        public ulong Entity { get; set; }
        public ulong Target {get; set; }
        public int Unk2 { get; set; }
        public string Name { get; set; }
        public ulong NameId { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public ulong UnkId1 { get; set; }
        public ulong UnkId2 { get; set; }

        public Destination(CodedInputStream stream)
        {
            Type = stream.ReadRawInt32();

            Region = stream.ReadPrototypeId(PrototypeEnumType.All);
            Area = stream.ReadPrototypeId(PrototypeEnumType.All);
            Cell = stream.ReadPrototypeId(PrototypeEnumType.All);
            Entity = stream.ReadPrototypeId(PrototypeEnumType.All);
            Target = stream.ReadPrototypeId(PrototypeEnumType.All);

            Unk2 = stream.ReadRawInt32();

            Name = stream.ReadRawString();
            NameId = stream.ReadRawVarint64();

            RegionId = stream.ReadRawVarint64();

            float x = stream.ReadRawFloat(); 
            float y = stream.ReadRawFloat();
            float z = stream.ReadRawFloat();
            Position = new Vector3(x, y, z);

            UnkId1 = stream.ReadRawVarint64();
            UnkId2 = stream.ReadRawVarint64();
        }

        public Destination(int type, ulong region, ulong area, ulong cell, ulong entity, ulong target, 
            int unk2, string name, ulong nameId, ulong regionId, 
            Vector3 position, ulong unkId1, ulong unkId2)
        {
            Type = type;
            Region = region;
            Area = area;
            Cell = cell;
            Entity = entity;
            Target = target;
            Unk2 = unk2;
            Name = name;
            NameId = nameId;
            RegionId = regionId;
            Position = position;
            UnkId1 = unkId1;
            UnkId2 = unkId2;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawInt32(Type);

                cos.WritePrototypeId(Region, PrototypeEnumType.All);
                cos.WritePrototypeId(Area, PrototypeEnumType.All);
                cos.WritePrototypeId(Cell, PrototypeEnumType.All);
                cos.WritePrototypeId(Entity, PrototypeEnumType.All);
                cos.WritePrototypeId(Target, PrototypeEnumType.All);

                cos.WriteRawInt32(Unk2);

                cos.WriteRawString(Name);
                cos.WriteRawVarint64(NameId);

                cos.WriteRawVarint64(RegionId);

                cos.WriteRawFloat(Position.X);
                cos.WriteRawFloat(Position.Y);
                cos.WriteRawFloat(Position.Z);

                cos.WriteRawVarint64(UnkId1);
                cos.WriteRawVarint64(UnkId2);

                cos.Flush();
                return ms.ToArray();
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Region: {GameDatabase.GetPrototypePath(Region)}");
            sb.AppendLine($"Area: {GameDatabase.GetPrototypePath(Area)}");
            sb.AppendLine($"Cell: {GameDatabase.GetPrototypePath(Cell)}");
            sb.AppendLine($"Entity: {GameDatabase.GetPrototypePath(Entity)}");
            sb.AppendLine($"Target: {GameDatabase.GetPrototypePath(Target)}");
            sb.AppendLine($"Unk2: {Unk2}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"NameId: {NameId}");
            sb.AppendLine($"RegionId: {RegionId}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"UnkId1: {UnkId1}");
            sb.AppendLine($"UnkId2: {UnkId2}");

            return sb.ToString();
        }
    }
}
