using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.DRAG.Generators.Regions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class Destination : ISerialize
    {
        private RegionTransitionType _type;
        private PrototypeId _regionRef;
        private PrototypeId _areaRef;
        private PrototypeId _cellRef;
        private PrototypeId _entityRef;
        private PrototypeId _targetRef;
        private int _unk2;
        private string _name;
        private LocaleStringId _nameId;
        private ulong _regionId;
        private Vector3 _position;
        private ulong _entityId;
        private ulong _unkId2;

        // TODO: Remove unnecessary accessors
        public RegionTransitionType Type { get => _type; set => _type = value; }
        public PrototypeId RegionRef { get => _regionRef; set => _regionRef = value; }
        public PrototypeId AreaRef { get => _areaRef; set => _areaRef = value; }
        public PrototypeId CellRef { get => _cellRef; set => _cellRef = value; }
        public PrototypeId EntityRef { get => _entityRef; set => _entityRef = value; }
        public PrototypeId TargetRef { get => _targetRef; set => _targetRef = value; }
        public int Unk2 { get => _unk2; set => _unk2 = value; }
        public string Name { get => _name; set => _name = value; }
        public LocaleStringId NameId { get => _nameId; set => _nameId = value; }
        public ulong RegionId { get => _regionId; set => _regionId = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public ulong UnkId2 { get => _unkId2; set => _unkId2 = value; }

        public Destination()
        {
            _position = Vector3.Zero;
            _name = string.Empty;
        }

        public Destination(RegionTransitionType type, PrototypeId regionRef, PrototypeId areaRef, PrototypeId cellRef, PrototypeId entityRef, PrototypeId targetRef,
            int unk2, string name, LocaleStringId nameId, ulong regionId,
            Vector3 position, ulong entityId, ulong unkId2)
        {
            _type = type;
            _regionRef = regionRef;
            _areaRef = areaRef;
            _cellRef = cellRef;
            _entityRef = entityRef;
            _targetRef = targetRef;
            _unk2 = unk2;
            _name = name;
            _nameId = nameId;
            _regionId = regionId;
            _position = position;
            _entityId = entityId;
            _unkId2 = unkId2;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            int type = (int)_type;
            success &= Serializer.Transfer(archive, ref type);
            _type = (RegionTransitionType)type;

            success &= Serializer.Transfer(archive, ref _regionRef);
            success &= Serializer.Transfer(archive, ref _areaRef);
            success &= Serializer.Transfer(archive, ref _cellRef);
            success &= Serializer.Transfer(archive, ref _entityRef);
            success &= Serializer.Transfer(archive, ref _targetRef);
            success &= Serializer.Transfer(archive, ref _unk2);
            success &= Serializer.Transfer(archive, ref _name);
            success &= Serializer.Transfer(archive, ref _nameId);
            success &= Serializer.Transfer(archive, ref _regionId);
            success &= Serializer.Transfer(archive, ref _position);
            success &= Serializer.Transfer(archive, ref _entityId);
            success &= Serializer.Transfer(archive, ref _unkId2);

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"{nameof(_type)}: {_type}");
            sb.AppendLine($"{nameof(_regionRef)}: {GameDatabase.GetPrototypeName(_regionRef)}");
            sb.AppendLine($"{nameof(_areaRef)}: {GameDatabase.GetPrototypeName(_areaRef)}");
            sb.AppendLine($"{nameof(_cellRef)}: {GameDatabase.GetPrototypeName(_cellRef)}");
            sb.AppendLine($"{nameof(_entityRef)}: {GameDatabase.GetPrototypeName(_entityRef)}");
            sb.AppendLine($"{nameof(_targetRef)}: {GameDatabase.GetPrototypeName(_targetRef)}");
            sb.AppendLine($"{nameof(_unk2)}: {_unk2}");
            sb.AppendLine($"{nameof(_name)}: {_name}");
            sb.AppendLine($"{nameof(_nameId)}: {_nameId}");
            sb.AppendLine($"{nameof(_regionId)}: {_regionId}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_unkId2)}: {_unkId2}");

            return sb.ToString();
        }

        public static Destination FindDestination(Cell cell, TransitionPrototype transitionProto)
        {
            if (cell == null) return null;
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
            
            if (RegionPrototype.Equivalent(targetRegion, region.RegionPrototype))
                targetRegionRef = (PrototypeId)region.PrototypeId;

            Destination destination = new()
            {
                _type = transitionProto.Type,
                _regionRef = targetRegionRef,
                _areaRef = regionConnectionTarget.Area,
                _cellRef = cellPrototypeId,
                _entityRef = regionConnectionTarget.Entity,
                _nameId = regionConnectionTarget.Name,
                _targetRef = targetRef
            };

            return destination;
        }
    }
}
