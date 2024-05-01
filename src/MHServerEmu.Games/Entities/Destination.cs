using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Destination
    {
        public RegionTransitionType Type { get; set; }
        public PrototypeId Region { get; set; }
        public PrototypeId Area { get; set; }
        public PrototypeId Cell { get; set; }
        public PrototypeId Entity { get; set; }
        public PrototypeId Target { get; set; }
        public int Unk2 { get; set; }
        public string Name { get; set; }
        public LocaleStringId NameId { get; set; }
        public ulong RegionId { get; set; }
        public Vector3 Position { get; set; }
        public ulong EntityId { get; set; }
        public ulong UnkId2 { get; set; }

        public Destination()
        {
            Position = Vector3.Zero;
            Name = "";
        }

        public Destination(CodedInputStream stream)
        {
            Type = (RegionTransitionType)stream.ReadRawInt32();

            Region = stream.ReadPrototypeRef<Prototype>();
            Area = stream.ReadPrototypeRef<Prototype>();
            Cell = stream.ReadPrototypeRef<Prototype>();
            Entity = stream.ReadPrototypeRef<Prototype>();
            Target = stream.ReadPrototypeRef<Prototype>();

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

            stream.WritePrototypeRef<Prototype>(Region);
            stream.WritePrototypeRef<Prototype>(Area);
            stream.WritePrototypeRef<Prototype>(Cell);
            stream.WritePrototypeRef<Prototype>(Entity);
            stream.WritePrototypeRef<Prototype>(Target);

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
            Region region = cell.Region;
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
