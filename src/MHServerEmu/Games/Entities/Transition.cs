using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.GameData;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Generators.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class Transition : WorldEntity
    {
        public string TransitionName { get; set; }
        public Destination[] Destinations { get; set; }

        public TransitionPrototype TransitionPrototype { get { return EntityPrototype as TransitionPrototype; } }
        public Transition(EntityBaseData baseData, ulong replicationId, ulong mapRegionId, int mapAreaId, int mapCellId, ulong contextAreaRef, 
            Vector3 mapPosition, Destination destination) : base(baseData)
        {
            ReplicationPolicy = 0x21;
            PropertyCollection = new(replicationId, new()
            {
                new(PropertyEnum.MapPosition, mapPosition),
                new(PropertyEnum.MapAreaId, mapAreaId),
                new(PropertyEnum.MapRegionId, mapRegionId),
                new(PropertyEnum.MapCellId, mapCellId),
                new(PropertyEnum.ContextAreaRef, contextAreaRef)
            });
            TrackingContextMap = Array.Empty<EntityTrackingContextMap>();
            ConditionCollection = Array.Empty<Condition>();
            PowerCollection = Array.Empty<PowerCollectionRecord>();
            UnkEvent = 0;

            TransitionName = "";
            Destinations = new Destination[1];
            Destinations[0] = destination;
        }

        public Transition(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

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

        protected override void Decode(CodedInputStream stream)
        {
            base.Decode(stream);

            TransitionName = stream.ReadRawString();

            Destinations = new Destination[stream.ReadRawVarint64()];
            for (int i = 0; i < Destinations.Length; i++)
                Destinations[i] = new(stream);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawString(TransitionName);
            stream.WriteRawVarint64((ulong)Destinations.Length);
            foreach (Destination destination in Destinations) destination.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"TransitionName: {TransitionName}");
            for (int i = 0; i < Destinations.Length; i++) sb.AppendLine($"Destination{i}: {Destinations[i]}");
        }

        public void ConfigureTowerGen(Transition transition)
        {
            // TODO: Elevators for Tower
            return;
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

        public Destination() { }
        public Destination(CodedInputStream stream)
        {
            Type = stream.ReadRawInt32();

            Region = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Area = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Cell = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Entity = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Target = stream.ReadPrototypeEnum(PrototypeEnumType.All);

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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawInt32(Type);

            stream.WritePrototypeEnum(Region, PrototypeEnumType.All);
            stream.WritePrototypeEnum(Area, PrototypeEnumType.All);
            stream.WritePrototypeEnum(Cell, PrototypeEnumType.All);
            stream.WritePrototypeEnum(Entity, PrototypeEnumType.All);
            stream.WritePrototypeEnum(Target, PrototypeEnumType.All);

            stream.WriteRawInt32(Unk2);

            stream.WriteRawString(Name);
            stream.WriteRawVarint64(NameId);

            stream.WriteRawVarint64(RegionId);

            stream.WriteRawFloat(Position.X);
            stream.WriteRawFloat(Position.Y);
            stream.WriteRawFloat(Position.Z);

            stream.WriteRawVarint64(UnkId1);
            stream.WriteRawVarint64(UnkId2);
        }
        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"Type: {Type}");
            sb.AppendLine($"Region: {GameDatabase.GetPrototypeName(Region)}");
            sb.AppendLine($"Area: {GameDatabase.GetPrototypeName(Area)}");
            sb.AppendLine($"Cell: {GameDatabase.GetPrototypeName(Cell)}");
            sb.AppendLine($"Entity: {GameDatabase.GetPrototypeName(Entity)}");
            sb.AppendLine($"Target: {GameDatabase.GetPrototypeName(Target)}");
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
