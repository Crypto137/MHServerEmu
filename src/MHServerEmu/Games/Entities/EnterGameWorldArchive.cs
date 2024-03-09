using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    // Unused flag names from old protocols:
    // isClientEntityHidden, newOnServer
    [Flags]
    public enum EnterGameWorldMessageFlags : uint
    {
        None                        = 0,
        HasAvatarWorldInstanceId    = 1 << 0,
        Flag1                       = 1 << 1,
        Flag2                       = 1 << 2,
        Flag3                       = 1 << 3
    }

    public class EnterGameWorldArchive
    {
        // Examples
        // Player:                       01B2F8FD06A021F0A301BC40902E9103BC05000001
        // Waypoint:                     010C028043E06BD82AC801
        // Something with flags 0xA0:    01BEF8FD06A001B6A501D454902E0094050000

        private const int LocoFlagsCount = 12;

        public AOINetworkPolicyValues ReplicationPolicy { get; }
        public ulong EntityId { get; set; }
        public LocomotionMessageFlags LocoFieldFlags { get; set; }
        public EnterGameWorldMessageFlags ExtraFieldFlags { get; set; }
        public PrototypeId EntityPrototypeId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }
        public uint AvatarWorldInstanceId { get; set; }     // This was signed in old protocols

        public EnterGameWorldArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();

            // This archive contains additional flags combined with LocomotionMessageFlags in a single 32-bit value
            uint allFieldFlags = stream.ReadRawVarint32();
            LocoFieldFlags = (LocomotionMessageFlags)(allFieldFlags & 0xFFF);
            ExtraFieldFlags = (EnterGameWorldMessageFlags)(allFieldFlags >> LocoFlagsCount);

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeId))
                EntityPrototypeId = stream.ReadPrototypeEnum<EntityPrototype>();

            Position = new(stream, 3);

            Orientation = LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                ? new(stream, 6)
                : new(stream.ReadRawZigZagFloat(6), 0f, 0f);

            if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                LocomotionState = new(stream, LocoFieldFlags);

            if (ExtraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
                AvatarWorldInstanceId = stream.ReadRawVarint32();
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            EntityId = entityId;
            LocoFieldFlags = LocomotionMessageFlags.NoLocomotionState;
            ExtraFieldFlags = EnterGameWorldMessageFlags.None;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
        }

        public EnterGameWorldArchive(ulong entityId, Vector3 position, float orientation, float moveSpeed)
        {
            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity;
            EntityId = entityId;
            LocoFieldFlags = LocomotionMessageFlags.UpdatePathNodes | LocomotionMessageFlags.HasMoveSpeed;
            ExtraFieldFlags = EnterGameWorldMessageFlags.HasAvatarWorldInstanceId;
            Position = position;
            Orientation = new(orientation, 0f, 0f);
            LocomotionState = new(moveSpeed);
            AvatarWorldInstanceId = 1;
        }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64((uint)ReplicationPolicy);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawVarint32((uint)LocoFieldFlags | ((uint)ExtraFieldFlags << LocoFlagsCount));     // Combine flags

                if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeId))
                    cos.WritePrototypeEnum<EntityPrototype>(EntityPrototypeId);

                Position.Encode(cos);

                if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    Orientation.Encode(cos);
                else
                    cos.WriteRawZigZagFloat(Orientation.Yaw, 6);

                if (LocoFieldFlags.HasFlag(LocomotionMessageFlags.NoLocomotionState) == false)
                    LocomotionState.Encode(cos, LocoFieldFlags);

                if (ExtraFieldFlags.HasFlag(EnterGameWorldMessageFlags.HasAvatarWorldInstanceId))
                    cos.WriteRawVarint32(AvatarWorldInstanceId);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"LocoFieldFlags: {LocoFieldFlags}");
            sb.AppendLine($"ExtraFieldFlags: {ExtraFieldFlags}");
            sb.AppendLine($"EntityPrototypeId: {GameDatabase.GetPrototypeName(EntityPrototypeId)}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            sb.AppendLine($"AvatarWorldInstanceId: {AvatarWorldInstanceId}");
            return sb.ToString();
        }
    }
}
