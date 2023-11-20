using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class LocomotionStateUpdateArchive
    {
        public AoiNetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public LocomotionMessageFlags FieldFlags { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }

        public LocomotionStateUpdateArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AoiNetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            FieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();

            if (FieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeId))
                PrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Entity);
            
            Position = new(stream, 3);

            Orientation = FieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                ? new(stream, 6)
                : new(stream.ReadRawZigZagFloat(6), 0f, 0f);

            LocomotionState = new(stream, FieldFlags);
        }

        public LocomotionStateUpdateArchive() { }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32((uint)ReplicationPolicy);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawVarint32((uint)FieldFlags);

                if (FieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeId))
                    cos.WritePrototypeEnum(PrototypeId, PrototypeEnumType.Entity);

                Position.Encode(cos, 3);

                if (FieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    Orientation.Encode(cos, 6);
                else
                    cos.WriteRawZigZagFloat(Orientation.X, 6);

                LocomotionState.Encode(cos, FieldFlags);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"FieldFlags: {FieldFlags}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            return sb.ToString();
        }
    }
}
