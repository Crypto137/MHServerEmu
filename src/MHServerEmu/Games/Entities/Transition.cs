using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Transition : WorldEntity
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public string TransitionName { get; set; }
        public List<Destination> Destinations { get; set; }

        public TransitionPrototype TransitionPrototype { get { return EntityPrototype as TransitionPrototype; } }

        public Transition(EntityBaseData baseData, ReplicatedPropertyCollection properties, Destination destination) : base(baseData)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity | AOINetworkPolicyValues.AOIChannelDiscovery;
            Properties = properties;
            TrackingContextMap = new();
            ConditionCollection = new();
            PowerCollection = new();
            UnkEvent = 0;

            TransitionName = "";
            Destinations = new();
            if (destination != null)
                Destinations.Add(destination);
        }

        public Transition(EntityBaseData baseData, ByteString archiveData) : base(baseData, archiveData) { }

        public Transition(EntityBaseData baseData, List<EntityTrackingContextMap> trackingContextMap, List<Condition> conditionCollection,
            List<PowerCollectionRecord> powerCollection, int unkEvent, 
            string transitionName, List<Destination> destinations) : base(baseData)
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

            int destinationsCount = (int)stream.ReadRawVarint64();
            for (int i = 0; i < destinationsCount; i++)
                Destinations.Add(new(stream));
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);

            stream.WriteRawString(TransitionName);
            stream.WriteRawVarint64((ulong)Destinations.Count);
            foreach (Destination destination in Destinations) destination.Encode(stream);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"TransitionName: {TransitionName}");
            for (int i = 0; i < Destinations.Count; i++) sb.AppendLine($"Destination{i}: {Destinations[i]}");
        }

        public void ConfigureTowerGen(Transition transition)
        {
            Destination destination;
            if (Destinations.Count == 0)
            {
                destination = new();
                Destinations.Add(destination);
            }
            else
            {
                destination = Destinations[0];
            }
            destination.EntityId = transition.BaseData.EntityId;
            destination.Entity = transition.BaseData.PrototypeId;
            destination.Type = TransitionPrototype.Type;
        }

        public void TeleportClient(FrontendClient client)
        {
            Logger.Trace($"Destination region {GameDatabase.GetFormattedPrototypeName(Destinations[0].Region)} [{GameDatabase.GetFormattedPrototypeName(Destinations[0].Entity)}]");
            client.CurrentGame.MovePlayerToRegion(client, (RegionPrototypeId)Destinations[0].Region, Destinations[0].Target);
        }

        public void TeleportToEntity(FrontendClient client, ulong entityId)
        {
            Logger.Trace($"Destination EntityId [{entityId}] [{GameDatabase.GetFormattedPrototypeName(Destinations[0].Entity)}]");
            client.CurrentGame.MovePlayerToEntity(client, Destinations[0].EntityId);
        }

        public void TeleportToLastTown(FrontendClient client)
        {
            // TODO back to last saved hub
            Logger.Trace($"Destination LastTown");
            client.CurrentGame.MovePlayerToRegion(client, RegionPrototypeId.AvengersTowerHUBRegion, (PrototypeId)10137590415717831231);
        }
    }

    public class Destination
    {
        public RegionTransitionType Type { get; set; }
        public PrototypeId Region { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId Cell { get; set; }
        public PrototypeId Entity { get; set; }
        public PrototypeId Target {get; set; }
        public int Unk2 { get; set; }
        public string Name { get; set; }
        public LocaleStringId NameId { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public ulong EntityId { get; set; }
        public ulong UnkId2 { get; set; }

        public Destination() { 
            Position = Vector3.Zero;
            Name = ""; 
        }

        public Destination(CodedInputStream stream)
        {
            Type = (RegionTransitionType)stream.ReadRawInt32();

            Region = stream.ReadPrototypeEnum<Prototype>();
            Area = stream.ReadPrototypeEnum<Prototype>();
            Cell = stream.ReadPrototypeEnum<Prototype>();
            Entity = stream.ReadPrototypeEnum<Prototype>();
            Target = stream.ReadPrototypeEnum<Prototype>();

            Unk2 = stream.ReadRawInt32();

            Name = stream.ReadRawString();
            NameId = (LocaleStringId)stream.ReadRawVarint64();

            RegionId = stream.ReadRawVarint64();

            float x = stream.ReadRawFloat(); 
            float y = stream.ReadRawFloat();
            float z = stream.ReadRawFloat();
            Position = new Vector3(x, y, z);

            EntityId = stream.ReadRawVarint64();
            UnkId2 = stream.ReadRawVarint64();
        }

        public Destination(RegionTransitionType type, PrototypeId region, PrototypeId area, PrototypeId cell, PrototypeId entity, PrototypeId target, 
            int unk2, string name, LocaleStringId nameId, ulong regionId, 
            Vector3 position, ulong entityId, ulong unkId2)
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
            EntityId = entityId;
            UnkId2 = unkId2;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawInt32((int)Type);

            stream.WritePrototypeEnum<Prototype>(Region);
            stream.WritePrototypeEnum<Prototype>(Area);
            stream.WritePrototypeEnum<Prototype>(Cell);
            stream.WritePrototypeEnum<Prototype>(Entity);
            stream.WritePrototypeEnum<Prototype>(Target);

            stream.WriteRawInt32(Unk2);

            stream.WriteRawString(Name);
            stream.WriteRawVarint64((ulong)NameId);

            stream.WriteRawVarint64(RegionId);

            stream.WriteRawFloat(Position.X);
            stream.WriteRawFloat(Position.Y);
            stream.WriteRawFloat(Position.Z);

            stream.WriteRawVarint64(EntityId);
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
            sb.AppendLine($"UnkId1: {EntityId}");
            sb.AppendLine($"UnkId2: {UnkId2}");

            return sb.ToString();
        }

        public static Destination FindDestination(Cell cell, TransitionPrototype transitionProto)
        {
            PrototypeId area = cell.Area.PrototypeDataRef;
            Region region = cell.GetRegion();
            PrototypeGuid entityGuid = GameDatabase.GetPrototypeGuid(transitionProto.DataRef);
            ConnectionNodeList targets = region.Targets;
            TargetObject node = RegionTransition.GetTargetNode(targets, area, cell.PrototypeId, entityGuid);
            if (node != null)
                return DestinationFromTarget(node.TargetId, region, transitionProto);
            return null;
        }

        public static Destination DestinationFromTarget(PrototypeId targetRef, Region region, TransitionPrototype transitionProto)
        {
            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);

            var cellAssetId = regionConnectionTarget.Cell;
            var cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            var targetRegionRef = regionConnectionTarget.Region;


            var targetRegion = GameDatabase.GetPrototype<RegionPrototype>(targetRegionRef);
            if (RegionPrototype.Equivalent(targetRegion, region.RegionPrototype)) targetRegionRef = (PrototypeId)region.PrototypeId;

            Destination destination = new()
            {
                Type = transitionProto.Type,
                Region = targetRegionRef,
                Area = regionConnectionTarget.Area,
                Cell = cellPrototypeId,
                Entity = regionConnectionTarget.Entity,
                NameId = regionConnectionTarget.Name,
                Target = targetRef
            };
            return destination;
        }
    }
}
