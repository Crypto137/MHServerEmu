using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class LocomotionStateUpdateArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public ulong EntityId { get; set; }
        public LocomotionMessageFlags FieldFlags { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }

        public LocomotionStateUpdateArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            EntityId = stream.ReadRawVarint64();
            FieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();

            if (FieldFlags.HasFlag(LocomotionMessageFlags.HasEntityPrototypeId))
                PrototypeId = stream.ReadPrototypeRef<EntityPrototype>();
            
            Position = new(stream);

            Orientation = FieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation)
                ? new(stream)
                : new(stream.ReadRawZigZagFloat(6), 0f, 0f);

            LocomotionState = new();
            LocomotionState.Decode(stream, FieldFlags);
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
                    cos.WritePrototypeRef<EntityPrototype>(PrototypeId);

                Position.Encode(cos);

                if (FieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                    Orientation.Encode(cos);
                else
                    cos.WriteRawZigZagFloat(Orientation.Yaw, 6);

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
