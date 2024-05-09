using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    [Flags]
    public enum EnterGameWorldMessageFlags : uint
    {
        None                        = 0,
        HasAvatarWorldInstanceId    = 1 << 0,
        IsNewOnServer               = 1 << 1,
        IsClientEntityHidden        = 1 << 2,
        HasAttachedEntities         = 1 << 3
    }

    public class EnterGameWorldArchive : ISerialize
    {
        private const int LocoFlagCount = 12;

        private AOINetworkPolicyValues _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
        private ulong _entityId;
        private LocomotionMessageFlags _locoFieldFlags;
        private EnterGameWorldMessageFlags _extraFieldFlags;
        private PrototypeId _entityPrototypeRef;
        private Vector3 _position = Vector3.Zero;
        private Orientation _orientation = Orientation.Zero;
        private LocomotionState _locomotionState;
        private uint _avatarWorldInstanceId;
        private List<ulong> _attachedEntityList = new();

        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public LocomotionMessageFlags LocoFieldFlags { get => _locoFieldFlags; set => _locoFieldFlags = value; }
        public EnterGameWorldMessageFlags ExtraFieldFlags { get => _extraFieldFlags; set => _extraFieldFlags = value; }
        public PrototypeId EntityPrototypeRef { get => _entityPrototypeRef; set => _entityPrototypeRef = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public Orientation Orientation { get => _orientation; set => _orientation = value; }
        public LocomotionState LocomotionState { get => _locomotionState; set => _locomotionState = value; }
        public uint AvatarWorldInstanceId { get => _avatarWorldInstanceId; set => _avatarWorldInstanceId = value; }
        public List<ulong> AttachedEntityList { get => _attachedEntityList; }

        public EnterGameWorldArchive() { }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float moveSpeed)
        {
            _entityId = entityId;
            _locoFieldFlags = LocomotionMessageFlags.UpdatePathNodes | LocomotionMessageFlags.HasMoveSpeed;
            _extraFieldFlags = EnterGameWorldMessageFlags.HasAvatarWorldInstanceId;
            _position = position;
            _orientation = new(orientation, 0f, 0f);
            _locomotionState = new(moveSpeed);
            _avatarWorldInstanceId = 1;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _entityId);

            // This archive contains additional flags combined with LocomotionMessageFlags in a single 32-bit value
            // TODO: build flags
            uint flags = (uint)_locoFieldFlags | ((uint)_extraFieldFlags << LocoFlagCount);
            success &= Serializer.Transfer(archive, ref flags);
            _locoFieldFlags = (LocomotionMessageFlags)(flags & 0xFFF);
            _extraFieldFlags = (EnterGameWorldMessageFlags)(flags >> LocoFlagCount);

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
                success &= Serializer.Transfer(archive, ref _entityPrototypeRef);

            success &= Serializer.TransferVectorFixed(archive, ref _position, 3);

            bool yawOnly = _locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            success &= Serializer.TransferOrientationFixed(archive, ref _orientation, yawOnly, 6);

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

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
                success &= Serializer.Transfer(archive, ref _avatarWorldInstanceId);

            // TODO: IsNewOnServer, IsClientEntityHidden

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAttachedEntities))
                success &= Serializer.Transfer(archive, ref _attachedEntityList);

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            _replicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            _entityId = stream.ReadRawVarint64();

            // This archive contains additional flags combined with LocomotionMessageFlags in a single 32-bit value
            uint allFieldFlags = stream.ReadRawVarint32();
            _locoFieldFlags = (LocomotionMessageFlags)(allFieldFlags & 0xFFF);
            _extraFieldFlags = (EnterGameWorldMessageFlags)(allFieldFlags >> LocoFlagCount);

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
                _entityPrototypeRef = stream.ReadPrototypeRef<EntityPrototype>();

            _position = new(stream, 3);

            _orientation = _locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                ? new(stream, 6)
                : new(stream.ReadRawZigZagFloat(6), 0f, 0f);

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
            {
                _locomotionState = new();
                _locomotionState.Decode(stream, LocoFieldFlags);
            }

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
                _avatarWorldInstanceId = stream.ReadRawVarint32();

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAttachedEntities))
            {
                _attachedEntityList.Clear();
                ulong numAttachedEntities = stream.ReadRawVarint64();
                for (ulong i = 0; i < numAttachedEntities; i++)
                    _attachedEntityList.Add(stream.ReadRawVarint64());
            }
        }

        public void Encode(CodedOutputStream cos)
        {
            cos.WriteRawVarint64((uint)_replicationPolicy);
            cos.WriteRawVarint64(_entityId);
            cos.WriteRawVarint32((uint)_locoFieldFlags | ((uint)_extraFieldFlags << LocoFlagCount));     // Combine flags

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
                cos.WritePrototypeRef<EntityPrototype>(_entityPrototypeRef);

            _position.Encode(cos);

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                _orientation.Encode(cos);
            else
                cos.WriteRawZigZagFloat(_orientation.Yaw, 6);

            if (_locoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                _locomotionState.Encode(cos, _locoFieldFlags);

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
                cos.WriteRawVarint32(_avatarWorldInstanceId);

            if (_extraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAttachedEntities))
            {
                cos.WriteRawVarint64((ulong)_attachedEntityList.Count);
                foreach (ulong entityId in _attachedEntityList)
                    cos.WriteRawVarint64(entityId);
            }
        }

        public ByteString ToByteString()
        {
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)ReplicationPolicy))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_locoFieldFlags)}: {_locoFieldFlags}");
            sb.AppendLine($"{nameof(_extraFieldFlags)}: {_extraFieldFlags}");
            sb.AppendLine($"{nameof(_entityPrototypeRef)}: {GameDatabase.GetPrototypeName(_entityPrototypeRef)}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_orientation)}: {_orientation}");
            sb.AppendLine($"{nameof(_locomotionState)}: {_locomotionState}");
            sb.AppendLine($"{nameof(_avatarWorldInstanceId)}: {_avatarWorldInstanceId}");
            for (int i = 0; i < _attachedEntityList.Count; i++)
                sb.AppendLine($"{nameof(_attachedEntityList)}[{i}]: {_attachedEntityList[i]}");
            return sb.ToString();
        }
    }
}
