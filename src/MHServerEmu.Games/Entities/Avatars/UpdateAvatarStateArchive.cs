using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class UpdateAvatarStateArchive : ISerialize
    {
        // This is a client -> server archive, serialized in CAvatar::UpdateServerAvatarState().
        // This used to be a regular protobuf message, but it was converted to archive in 1.25.

        private AOINetworkPolicyValues _replicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
        private int _avatarIndex;
        private ulong _entityId;
        private bool _isUsingGamepadInput;
        private uint _avatarWorldInstanceId;
        private LocomotionMessageFlags _fieldFlags;
        private Vector3 _position = Vector3.Zero;
        private Orientation _orientation = Orientation.Zero;
        private LocomotionState _locomotionState = new();

        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public int AvatarIndex { get => _avatarIndex; set => _avatarIndex = value; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public bool IsUsingGamepadInput { get => _isUsingGamepadInput; set => _isUsingGamepadInput = value; }
        public uint AvatarWorldInstanceId { get => _avatarWorldInstanceId; set => _avatarWorldInstanceId = value; }
        public LocomotionMessageFlags FieldFlags { get => _fieldFlags; set => _fieldFlags = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public Orientation Orientation { get => _orientation; set => _orientation = value; }
        public LocomotionState LocomotionState { get => _locomotionState; set => _locomotionState = value; }

        public UpdateAvatarStateArchive() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _avatarIndex);
            success &= Serializer.Transfer(archive, ref _entityId);
            success &= Serializer.Transfer(archive, ref _isUsingGamepadInput);
            success &= Serializer.Transfer(archive, ref _avatarWorldInstanceId);

            uint flags = (uint)_fieldFlags;
            success &= Serializer.Transfer(archive, ref flags);
            _fieldFlags = (LocomotionMessageFlags)flags;

            success &= Serializer.TransferVectorFixed(archive, ref _position, 3);

            bool yawOnly = _fieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation) == false;
            success &= Serializer.TransferOrientationFixed(archive, ref _orientation, yawOnly, 6);

            if (archive.IsPacking)
                success &= LocomotionState.SerializeTo(archive, _locomotionState, _fieldFlags);
            else
                success &= LocomotionState.SerializeFrom(archive, _locomotionState, _fieldFlags);

            return success;
        }

        public ByteString ToByteString()
        {
            // The client always uses Proximity for this archive
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.AOIChannelProximity))
            {
                Serialize(archive);
                return archive.ToByteString();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_replicationPolicy)}: {_replicationPolicy}");
            sb.AppendLine($"{nameof(_avatarIndex)}: {_avatarIndex}");
            sb.AppendLine($"{nameof(_entityId)}: {_entityId}");
            sb.AppendLine($"{nameof(_isUsingGamepadInput)}: {_isUsingGamepadInput}");
            sb.AppendLine($"{nameof(_avatarWorldInstanceId)}: {_avatarWorldInstanceId}");
            sb.AppendLine($"{nameof(_fieldFlags)}: {_fieldFlags}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_orientation)}: {_orientation}");
            sb.AppendLine($"{nameof(_locomotionState)}: {_locomotionState}");
            return sb.ToString();
        }
    }
}
