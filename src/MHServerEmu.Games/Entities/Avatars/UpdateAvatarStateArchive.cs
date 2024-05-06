using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class UpdateAvatarStateArchive
    {
        public AOINetworkPolicyValues ReplicationPolicy { get; set; }
        public int AvatarIndex { get; set; }
        public ulong EntityId { get; set; }
        public bool IsUsingGamepadInput { get; set; }
        public uint AvatarWorldInstanceId { get; set; }
        public LocomotionMessageFlags FieldFlags { get; set; }
        public Vector3 Position { get; set; }
        public Orientation Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }

        public UpdateAvatarStateArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());
            BoolDecoder boolDecoder = new();

            ReplicationPolicy = (AOINetworkPolicyValues)stream.ReadRawVarint32();
            AvatarIndex = stream.ReadRawInt32();
            EntityId = stream.ReadRawVarint64();
            IsUsingGamepadInput = boolDecoder.ReadBool(stream);
            AvatarWorldInstanceId = stream.ReadRawVarint32();
            FieldFlags = (LocomotionMessageFlags)stream.ReadRawVarint32();
            Position = new(stream);
            
            if (FieldFlags.HasFlag(LocomotionMessageFlags.HasFullOrientation))
                Orientation = new(stream);
            else
                Orientation = new(stream.ReadRawZigZagFloat(6), 0f, 0f);
            
            LocomotionState = new();
            LocomotionState.Decode(stream, FieldFlags);
        }

        public UpdateAvatarStateArchive() { }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                // Prepare bool encoder
                BoolEncoder boolEncoder = new();
                boolEncoder.EncodeBool(IsUsingGamepadInput);
                boolEncoder.Cook();

                // Encode
                cos.WriteRawVarint32((uint)ReplicationPolicy);
                cos.WriteRawInt32(AvatarIndex);
                cos.WriteRawVarint64(EntityId);
                boolEncoder.WriteBuffer(cos);   // IsUsingGamepadInput  
                cos.WriteRawVarint32(AvatarWorldInstanceId);
                cos.WriteRawVarint32((uint)FieldFlags);
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
            sb.AppendLine($"AvatarIndex: {AvatarIndex}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"IsUsingGamepadInput: {IsUsingGamepadInput}");
            sb.AppendLine($"AvatarWorldInstanceId: {AvatarWorldInstanceId}");
            sb.AppendLine($"FieldFlags: {FieldFlags}");
            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            return sb.ToString();
        }
    }
}
