using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Network.Parsing.LegacyArchives
{
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

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float moveSpeed, bool isClientEntityHidden = false)
        {
            _entityId = entityId;
            _locoFieldFlags = LocomotionMessageFlags.UpdatePathNodes | LocomotionMessageFlags.HasMoveSpeed;
            _extraFieldFlags = EnterGameWorldMessageFlags.HasAvatarWorldInstanceId;
            _position = position;
            _orientation = new(orientation, 0f, 0f);
            _locomotionState = new() { BaseMoveSpeed = moveSpeed };
            _avatarWorldInstanceId = 1;

            if (isClientEntityHidden)
                _extraFieldFlags |= EnterGameWorldMessageFlags.IsClientEntityHidden;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _entityId);

            // This archive contains additional flags combined with LocomotionMessageFlags in a single 32-bit value
            // TODO: build flags
            uint flags = (uint)_locoFieldFlags | (uint)_extraFieldFlags << LocoFlagCount;
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
