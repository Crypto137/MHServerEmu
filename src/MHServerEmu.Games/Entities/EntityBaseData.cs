using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
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

    public class EntityBaseData
    {
        // This used to be a regular protobuf message, but it was converted to archive in 1.25.
        // This data is deserialized into EntitySettings in GameConnection::handleEntityCreateMessage().

        private AOINetworkPolicyValues _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
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
        private InventoryLocation _invLoc;
        private InventoryLocation _invLocPrev;
        private ulong[] _attachedEntities = Array.Empty<ulong>();   // TODO: Change to list?

        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
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
        public ulong[] AttachedEntities { get => _attachedEntities; }

        public EntityBaseData() { }

        public EntityBaseData(ulong entityId, PrototypeId prototypeId, Vector3 position, Orientation orientation, bool snap = false)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelDiscovery;
            EntityId = entityId;
            EntityPrototypeRef = prototypeId;
            LocomotionState = new(0f);

            FieldFlags = EntityCreateMessageFlags.None;
            LocoFieldFlags = LocomotionMessageFlags.None;

            if (position != null && orientation != null)
            {
                Position = position;
                Orientation = orientation;
                FieldFlags |= EntityCreateMessageFlags.HasPositionAndOrientation;
            }

            if (snap) FieldFlags |= EntityCreateMessageFlags.OverrideSnapToFloorOnSpawn;
        }

        public void Decode(CodedInputStream stream)
        {
            _replicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint64();
            _entityId = stream.ReadRawVarint64();
            _entityPrototypeRef = stream.ReadPrototypeRef<EntityPrototype>();
            _fieldFlags = (EntityCreateMessageFlags)stream.ReadRawVarint32();
            _locoFieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();

            _interestPolicies = FieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest)
                ? (AOINetworkPolicyValues)stream.ReadRawVarint32()
                : AOINetworkPolicyValues.AOIChannelProximity;

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                _avatarWorldInstanceId = stream.ReadRawVarint32();

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                _dbId = stream.ReadRawVarint32();

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                _position = new(stream);

                _orientation = LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                    ? new(stream)
                    : new(stream.ReadRawZigZagFloat(6), 0f, 0f);
            }

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
            {
                _locomotionState = new();
                _locomotionState.Decode(stream, _locoFieldFlags);
            }

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                _boundsScaleOverride = stream.ReadRawZigZagFloat(8);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                _sourceEntityId = stream.ReadRawVarint64();

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                _sourcePosition = new(stream);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
                _activePowerPrototypeRef = stream.ReadPrototypeRef<PowerPrototype>();

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                _invLoc = new(stream);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                _invLocPrev = new(stream);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
            {
                _attachedEntities = new ulong[stream.ReadRawVarint64()];
                for (int i = 0; i < _attachedEntities.Length; i++)
                    _attachedEntities[i] = stream.ReadRawVarint64();
            }
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)_replicationPolicy);
            stream.WriteRawVarint64(_entityId);
            stream.WritePrototypeRef<EntityPrototype>(_entityPrototypeRef);
            stream.WriteRawVarint32((uint)_fieldFlags);
            stream.WriteRawVarint32((uint)_locoFieldFlags);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasNonProximityInterest))
                stream.WriteRawVarint32((uint)_interestPolicies);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasAvatarWorldInstanceId))
                stream.WriteRawVarint32(_avatarWorldInstanceId);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasDbId))
                stream.WriteRawVarint64(_dbId);

            // Location
            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasPositionAndOrientation))
            {
                _position.Encode(stream);

                if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    _orientation.Encode(stream);
                else
                    stream.WriteRawZigZagFloat(_orientation.Yaw, 6);
            }

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                _locomotionState.Encode(stream, _locoFieldFlags);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasBoundsScaleOverride))
                stream.WriteRawZigZagFloat(_boundsScaleOverride, 8);

            if (_fieldFlags.HasFlag(EntityCreateMessageFlags.HasSourceEntityId))
                stream.WriteRawVarint64(_sourceEntityId);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasSourcePosition))
                _sourcePosition.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasActivePowerPrototypeRef))
                stream.WritePrototypeRef<PowerPrototype>(_activePowerPrototypeRef);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLoc))
                _invLoc.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasInvLocPrev))
                _invLocPrev.Encode(stream);

            if (FieldFlags.HasFlag(EntityCreateMessageFlags.HasAttachedEntities))
            {
                stream.WriteRawVarint64((ulong)_attachedEntities.Length);
                for (int i = 0; i < _attachedEntities.Length; i++)
                    stream.WriteRawVarint64(_attachedEntities[i]);
            }
        }

        public ByteString ToByteString()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_entityPrototypeRef)}: {GameDatabase.GetPrototypeName(_entityPrototypeRef)}");
            sb.AppendLine($"{nameof(_fieldFlags)}: {_fieldFlags}");
            sb.AppendLine($"{nameof(_locoFieldFlags)}: {_locoFieldFlags}");
            sb.AppendLine($"{nameof(_interestPolicies)}: {_interestPolicies}");
            sb.AppendLine($"{nameof(_avatarWorldInstanceId)}: {_avatarWorldInstanceId}");
            sb.AppendLine($"{nameof(_dbId)}: {_dbId}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_orientation)}: {_orientation}");
            sb.AppendLine($"{nameof(_locomotionState)}: {_locomotionState}");
            sb.AppendLine($"{nameof(_boundsScaleOverride)}: {_boundsScaleOverride}");
            sb.AppendLine($"{nameof(_sourceEntityId)}: {_sourceEntityId}");
            sb.AppendLine($"{nameof(_sourcePosition)}: {_sourcePosition}");
            sb.AppendLine($"{nameof(_activePowerPrototypeRef)}: {GameDatabase.GetPrototypeName(_activePowerPrototypeRef)}");
            sb.AppendLine($"{nameof(_invLoc)}: {_invLoc}");
            sb.AppendLine($"{nameof(_invLocPrev)}: {_invLocPrev}");
            for (int i = 0; i < _attachedEntities.Length; i++)
                sb.AppendLine($"{nameof(_attachedEntities)}[{i}]: {_attachedEntities[i]}");
            return sb.ToString();
        }
    }
}
