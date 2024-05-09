using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class LocomotionStateUpdateArchive : ISerialize
    {
        private AOINetworkPolicyValues _replicationPolicy;
        private ulong _entityId;
        private LocomotionMessageFlags _fieldFlags;
        private PrototypeId _entityPrototypeRef;
        private Vector3 _position = Vector3.Zero;
        private Orientation _orientation = Orientation.Zero;
        private LocomotionState _locomotionState = new();

        public AOINetworkPolicyValues ReplicationPolicy { get => _replicationPolicy; set => _replicationPolicy = value; }
        public ulong EntityId { get => _entityId; set => _entityId = value; }
        public LocomotionMessageFlags FieldFlags { get => _fieldFlags; set => _fieldFlags = value; }
        public PrototypeId EntityPrototypeRef { get => _entityPrototypeRef; set => _entityPrototypeRef = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public Orientation Orientation { get => _orientation; set => _orientation = value; }
        public LocomotionState LocomotionState { get => _locomotionState; set => _locomotionState = value; }

        public LocomotionStateUpdateArchive() { }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            // NOTE: Deserialized in GameConnection::handleLocomotionStateUpdateMessage() in the client
            success &= Serializer.Transfer(archive, ref _entityId);

            uint flags = (uint)_fieldFlags;
            success &= Serializer.Transfer(archive, ref flags);
            _fieldFlags = (LocomotionMessageFlags)flags;

            if (_fieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeRef))
                success &= Serializer.TransferPrototypeEnum<EntityPrototype>(archive, ref _entityPrototypeRef);

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
            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)_replicationPolicy))
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
            sb.AppendLine($"{nameof(_fieldFlags)}: {_fieldFlags}");
            sb.AppendLine($"{nameof(_entityPrototypeRef)}: {GameDatabase.GetPrototypeName(_entityPrototypeRef)}");
            sb.AppendLine($"{nameof(_position)}: {_position}");
            sb.AppendLine($"{nameof(_orientation)}: {_orientation}");
            sb.AppendLine($"{nameof(_locomotionState)}: {_locomotionState}");
            return sb.ToString();
        }
    }
}
