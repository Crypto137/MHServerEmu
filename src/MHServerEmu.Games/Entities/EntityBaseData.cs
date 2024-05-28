using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    // Unused bool field names from 1.24: initConditionComponent, startFullInWorldHierarchyUpdate
    [Flags]
    public enum EntityCreateMessageFlags : uint
    {
        None                        = 0,
        HasPositionAndOrientation   = 1 << 0,
        HasActivePowerPrototypeRef  = 1 << 1,
        IsNewOnServer               = 1 << 2,
        HasSourceEntityId           = 1 << 3,
        HasSourcePosition           = 1 << 4,
        HasNonProximityInterest     = 1 << 5,
        HasInvLoc                   = 1 << 6,
        HasInvLocPrev               = 1 << 7,
        HasDbId                     = 1 << 8,
        HasAvatarWorldInstanceId    = 1 << 9,
        OverrideSnapToFloorOnSpawn  = 1 << 10,
        HasBoundsScaleOverride      = 1 << 11,
        IsClientEntityHidden        = 1 << 12,
        Flag13                      = 1 << 13,  // Unused
        HasAttachedEntities         = 1 << 14,
        IgnoreNavi                  = 1 << 15
    }

    public class EntityBaseData : ISerialize
    {
        // This used to be a regular protobuf message, but it was converted to archive in 1.25.
        // This data is deserialized into EntitySettings in GameConnection::handleEntityCreateMessage().

        // TODO: Convert this to a protobuf-style builder or remove it entirely.

        private ulong _entityId;
        private PrototypeId _entityPrototypeRef;
        private EntityCreateMessageFlags _fieldFlags;
        private LocomotionMessageFlags _locoFieldFlags;
        private AOINetworkPolicyValues _interestPolicies = AOINetworkPolicyValues.AOIChannelProximity;
        private uint _avatarWorldInstanceId;
        private ulong _dbId;
        private Vector3 _position = Vector3.Zero;
        private Orientation _orientation = Orientation.Zero;
        private LocomotionState _locomotionState = new();
        private float _boundsScaleOverride;
        private ulong _sourceEntityId;
        private Vector3 _sourcePosition = Vector3.Zero;
        private PrototypeId _activePowerPrototypeRef;
        private InventoryLocation _invLoc = new();
        private InventoryLocation _invLocPrev;
        private List<ulong> _attachedEntityList = new();

        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public PrototypeId EntityPrototypeRef { get => _entityPrototypeRef; set => _entityPrototypeRef = value; }
        public EntityCreateMessageFlags FieldFlags { get => _fieldFlags; set => _fieldFlags = value; }
        public LocomotionMessageFlags LocoFieldFlags { get => _locoFieldFlags; set => _locoFieldFlags = value; }
        public AOINetworkPolicyValues InterestPolicies { get => _interestPolicies; set => _interestPolicies = value; }
        public uint AvatarWorldInstanceId { get => _avatarWorldInstanceId; set => _avatarWorldInstanceId = value; }
        public ulong DbId { get => _dbId; set => _dbId = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public Orientation Orientation { get => _orientation; set => _orientation = value; }
        public LocomotionState LocomotionState { get => _locomotionState; set => _locomotionState = value; }
        public float BoundsScaleOverride { get => _boundsScaleOverride; }
        public ulong SourceEntityId { get => _sourceEntityId; }
        public Vector3 SourcePosition { get => _sourcePosition; }
        public PrototypeId ActivePowerPrototypeRef { get => _activePowerPrototypeRef; }
        public InventoryLocation InvLoc { get => _invLoc; set => _invLoc = value; }
        public InventoryLocation InvLocPrev { get => _invLocPrev; set => _invLocPrev = value; }
        public List<ulong> AttachedEntityList { get => _attachedEntityList; }

        public EntityBaseData() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // TODO: build flags

            success &= Serializer.Transfer(archive, ref _entityId);
            success &= Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref _entityPrototypeRef);

            uint fieldFlags = (uint)_fieldFlags;
            success &= Serializer.Transfer(archive, ref fieldFlags);
            _fieldFlags = (EntityCreateMessageFlags)fieldFlags;

            uint locoFieldFlags = (uint)_locoFieldFlags;
            success &= Serializer.Transfer(archive, ref locoFieldFlags);
            _locoFieldFlags = (LocomotionMessageFlags)locoFieldFlags;

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest))
            {
                uint interestPolicies = (uint)_interestPolicies;
                success &= Serializer.Transfer(archive, ref interestPolicies);
                _interestPolicies = (AOINetworkPolicyValues)interestPolicies;
            }
            else if (archive.IsUnpacking)
                _interestPolicies = AOINetworkPolicyValues.AOIChannelProximity;

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                success &= Serializer.Transfer(archive, ref _avatarWorldInstanceId);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                success &= Serializer.Transfer(archive, ref _dbId);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                success &= Serializer.TransferVectorFixed(archive, ref _position, 3);

                bool yawOnly = _locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
                success &= Serializer.TransferOrientationFixed(archive, ref _orientation, yawOnly, 6);
            }

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
            {
                if (archive.IsPacking)
                    success &= LocomotionState.SerializeTo(archive, _locomotionState, _locoFieldFlags);
                else
                {
                    if (_locomotionState == null) _locomotionState = new();
                    success &= LocomotionState.SerializeFrom(archive, _locomotionState, _locoFieldFlags);
                }
            }

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                success &= Serializer.TransferFloatFixed(archive, ref _boundsScaleOverride, 8);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                success &= Serializer.Transfer(archive, ref _sourceEntityId);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                success &= Serializer.TransferVectorFixed(archive, ref _sourcePosition, 3);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
                success &= Serializer.TransferPrototypeEnum<PowerPrototype>(archive, ref _activePowerPrototypeRef);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
            {
                if (archive.IsPacking)
                    success &= InventoryLocation.SerializeTo(archive, _invLoc);
                else
                {
                    if (_invLoc == null) _invLoc = new();
                    success &= InventoryLocation.SerializeFrom(archive, _invLoc);
                }
            }

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
            {
                if (archive.IsPacking)
                    success &= InventoryLocation.SerializeTo(archive, _invLocPrev);
                else
                {
                    if (_invLocPrev == null) _invLocPrev = new();
                    success &= InventoryLocation.SerializeFrom(archive, _invLocPrev);
                }
            }

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
                success &= Serializer.Transfer(archive, ref _attachedEntityList);

            return success;
        }

        public ByteString ToByteString()
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)InterestPolicies))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_entityPrototypeRef)}: {GameDatabase.GetPrototypeName(_entityPrototypeRef)}");
            sb.AppendLine($"{nameof(_fieldFlags)}: {_fieldFlags}");
            sb.AppendLine($"{nameof(_locoFieldFlags)}: {_locoFieldFlags}");
            sb.AppendLine($"{nameof(_interestPolicies)}: {_interestPolicies}");
            sb.AppendLine($"{nameof(_avatarWorldInstanceId)}: {_avatarWorldInstanceId}");
            sb.AppendLine($"{nameof(_dbId)}: 0x{_dbId:X}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_orientation)}: {_orientation}");
            sb.AppendLine($"{nameof(_locomotionState)}: {_locomotionState}");
            sb.AppendLine($"{nameof(_boundsScaleOverride)}: {_boundsScaleOverride}");
            sb.AppendLine($"{nameof(_sourceEntityId)}: {_sourceEntityId}");
            sb.AppendLine($"{nameof(_sourcePosition)}: {_sourcePosition}");
            sb.AppendLine($"{nameof(_activePowerPrototypeRef)}: {GameDatabase.GetPrototypeName(_activePowerPrototypeRef)}");
            sb.AppendLine($"{nameof(_invLoc)}: {_invLoc}");
            sb.AppendLine($"{nameof(_invLocPrev)}: {_invLocPrev}");
            for (int i = 0; i < _attachedEntityList.Count; i++)
                sb.AppendLine($"{nameof(_attachedEntityList)}[{i}]: {_attachedEntityList[i]}");
            return sb.ToString();
        }
    }
}
