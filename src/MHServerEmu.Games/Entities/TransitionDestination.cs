using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private int _type;     // This is some other type enum, not RegionTransitionType.
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

        public PrototypeId RegionRef { get => _regionRef; }
        public PrototypeId AreaRef { get => _areaRef; }
        public PrototypeId CellRef { get => _cellRef; }
        public PrototypeId EntityRef { get => _entityRef; }
        public PrototypeId TargetRef { get => _targetRef; }
        public int UISortOrder { get => _uiSortOrder; }
        public ulong RegionId { get => _regionId; }
        public Vector3 Position { get => _position; }
        public ulong EntityId { get => _entityId; }

        public TransitionDestination()
        {
            _position = Vector3.Zero;
            _name = string.Empty;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _type);
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

        public LocaleStringId GetDisplayName()
        {
            if (_nameId != LocaleStringId.Invalid)
                return _nameId;

            if (_areaRef != PrototypeId.Invalid)
            {
                AreaPrototype areaProto = _areaRef.As<AreaPrototype>();
                if (areaProto != null && areaProto.AreaName != LocaleStringId.Invalid)
                    return areaProto.AreaName;
            }

            if (_regionRef != PrototypeId.Invalid)
            {
                RegionPrototype regionProto = _regionRef.As<RegionPrototype>();
                if (regionProto != null && regionProto.RegionName != LocaleStringId.Invalid)
                    return regionProto.RegionName;
            }

            return LocaleStringId.Invalid;
        }

        public void SetEntity(Entity entity)
        {
            _entityRef = entity.PrototypeDataRef;
            _entityId = entity.Id;
        }

        public bool IsAvailable(Player player)
        {
            // Non-target based destinations are always available.
            if (_targetRef == PrototypeId.Invalid)
                return true;

            // Check interaction manager for mission-based availability.
            if (GameDatabase.InteractionManager.GetRegionTargetAvailability(player, _targetRef, out bool isAvailable))
                return isAvailable;

            // Default to target prototype data.
            RegionConnectionTargetPrototype targetProto = _targetRef.As<RegionConnectionTargetPrototype>();
            if (targetProto == null) return Logger.WarnReturn(false, "IsAvailable(): targetProto == null");
            return targetProto.EnabledByDefault;
        }

        public static bool AddDestinationsFromConnectionTargets(Cell cell, TransitionPrototype transitionProto, List<TransitionDestination> outDestinations)
        {
            if (cell == null)
                return false;

            PrototypeId cellRef = cell.PrototypeDataRef;
            PrototypeId area = cell.Area.PrototypeDataRef;
            Region region = cell.Region;
            PrototypeGuid entityGuid = GameDatabase.GetPrototypeGuid(transitionProto.DataRef);

            bool added = false;

            foreach (TargetObject targetNode in region.Targets)
            {
                if (targetNode.Entity != entityGuid)
                    continue;

                if (targetNode.Area != PrototypeId.Invalid && targetNode.Area != area)
                    continue;

                if (targetNode.Cell != PrototypeId.Invalid && targetNode.Cell != cellRef)
                    continue;

                TransitionDestination destination = FromTarget(targetNode.TargetId, region, transitionProto);
                outDestinations.Add(destination);
                added = true;
            }

            return added;
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
                _regionRef = targetRegionRef,
                _areaRef = regionConnectionTarget.Area,
                _cellRef = cellPrototypeId,
                _entityRef = regionConnectionTarget.Entity,
                _nameId = regionConnectionTarget.Name,
                _targetRef = targetRef,
                _uiSortOrder = regionConnectionTarget.UISortOrder,
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
                _regionRef = proto.Region,
                _areaRef = proto.Area,
                _cellRef = cellPrototypeId,
                _entityRef = proto.Entity,
                _nameId = proto.Name,
                _targetRef = targetRef,
                _uiSortOrder = proto.UISortOrder,
            };

            return destination;
        }

        public static TransitionDestination FromRegionOrigin(NetStructRegionOrigin origin)
        {
            // Null is potentially valid input here.
            if (origin == null)
                return null;

            NetStructRegionTarget target = origin.Target;
            NetStructRegionLocation location = origin.Location;

            TransitionDestination destination = new()
            {
                _regionRef = (PrototypeId)target.RegionProtoId,
                _areaRef = (PrototypeId)target.AreaProtoId,
                _cellRef = (PrototypeId)target.CellProtoId,
                _entityRef = (PrototypeId)target.EntityProtoId,

                _regionId = location.RegionId,
                _position = new(location.Position),
            };

            return destination;
        }
    }
}
