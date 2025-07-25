using System.Text;
using Gazillion;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.DRAG.Generators.Regions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Entities
{
    public class TransitionDestination : ISerialize
    {
        private RegionTransitionType _type;
        private PrototypeId _regionRef;
        private PrototypeId _areaRef;
        private PrototypeId _cellRef;
        private PrototypeId _entityRef;
        private PrototypeId _targetRef;
        private int _uiSortOrder;
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
        public int UISortOrder { get => _uiSortOrder; set => _uiSortOrder = value; }
        public string Name { get => _name; set => _name = value; }
        public LocaleStringId NameId { get => _nameId; set => _nameId = value; }
        public ulong RegionId { get => _regionId; set => _regionId = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public ulong UnkId2 { get => _unkId2; set => _unkId2 = value; }

        public TransitionDestination()
        {
            _position = Vector3.Zero;
            _name = string.Empty;
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
            success &= Serializer.Transfer(archive, ref _uiSortOrder);
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
            sb.AppendLine($"{nameof(_regionRef)}: {_regionRef.GetName()}");
            sb.AppendLine($"{nameof(_areaRef)}: {_areaRef.GetName()}");
            sb.AppendLine($"{nameof(_cellRef)}: {_cellRef.GetName()}");
            sb.AppendLine($"{nameof(_entityRef)}: {_entityRef.GetName()}");
            sb.AppendLine($"{nameof(_targetRef)}: {_targetRef.GetName()}");
            sb.AppendLine($"{nameof(_uiSortOrder)}: {_uiSortOrder}");
            sb.AppendLine($"{nameof(_name)}: {_name}");
            sb.AppendLine($"{nameof(_nameId)}: {_nameId}");
            sb.AppendLine($"{nameof(_regionId)}: {_regionId}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_unkId2)}: {_unkId2}");

            return sb.ToString();
        }

        public static TransitionDestination Find(Cell cell, TransitionPrototype transitionProto)
        {
            if (cell == null) return null;

            // NOTE: Adding a destination to some waypoints makes them unusable
            if (transitionProto.Type == RegionTransitionType.Waypoint) return null;

            PrototypeId area = cell.Area.PrototypeDataRef;
            Region region = cell.Region;
            PrototypeGuid entityGuid = GameDatabase.GetPrototypeGuid(transitionProto.DataRef);

            TargetObject node = RegionTransition.GetTargetNode(region.Targets, area, cell.PrototypeDataRef, entityGuid);
            if (node == null) return null;

            return FromTarget(node.TargetId, region, transitionProto);
        }

        public static TransitionDestination FromTarget(PrototypeId targetRef, Region region, TransitionPrototype transitionProto)
        {
            var regionConnectionTarget = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);

            AssetId cellAssetId = regionConnectionTarget.Cell;
            PrototypeId cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            PrototypeId targetRegionRef = regionConnectionTarget.Region;
            var targetRegionProto = GameDatabase.GetPrototype<RegionPrototype>(targetRegionRef);
            
            if (RegionPrototype.Equivalent(targetRegionProto, region.Prototype))
                targetRegionRef = region.PrototypeDataRef;

            TransitionDestination destination = new()
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

        public static TransitionDestination FromTargetRef(PrototypeId targetRef)
        {
            var proto = GameDatabase.GetPrototype<RegionConnectionTargetPrototype>(targetRef);
            AssetId cellAssetId = proto.Cell;
            PrototypeId cellPrototypeId = cellAssetId != AssetId.Invalid ? GameDatabase.GetDataRefByAsset(cellAssetId) : PrototypeId.Invalid;

            TransitionDestination destination = new()
            {
                _type = RegionTransitionType.TransitionDirect,
                _regionRef = proto.Region,
                _areaRef = proto.Area,
                _cellRef = cellPrototypeId,
                _entityRef = proto.Entity,
                _nameId = proto.Name,
                _targetRef = targetRef
            };

            return destination;
        }

        public static TransitionDestination FromRegionOrigin(NetStructRegionOrigin origin)
        {
            NetStructRegionTarget target = origin.Target;
            NetStructRegionLocation location = origin.Location;

            TransitionDestination destination = new()
            {
                _type = RegionTransitionType.TransitionDirectReturn,

                RegionRef = (PrototypeId)target.RegionProtoId,
                AreaRef = (PrototypeId)target.AreaProtoId,
                CellRef = (PrototypeId)target.CellProtoId,
                EntityRef = (PrototypeId)target.EntityProtoId,

                RegionId = location.RegionId,
                Position = new(location.Position),
            };

            return destination;
        }
    }
}
