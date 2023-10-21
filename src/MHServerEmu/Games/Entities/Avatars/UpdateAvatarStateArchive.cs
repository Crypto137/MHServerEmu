using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities.Locomotion;

namespace MHServerEmu.Games.Entities.Avatars
{
    public class UpdateAvatarStateArchive
    {
        private const int LocFlagCount = 16;

        public uint ReplicationPolicy { get; set; }
        public int AvatarIndex { get; set; }
        public ulong EntityId { get; set; }
        public bool IsUsingGamepadInput { get; set; }
        public uint AvatarWorldInstanceId { get; set; }
        public bool[] LocFlags { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Orientation { get; set; }
        public LocomotionState LocomotionState { get; set; }

        public UpdateAvatarStateArchive(ByteString data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data.ToByteArray());
            BoolDecoder boolDecoder = new();

            ReplicationPolicy = stream.ReadRawVarint32();
            AvatarIndex = stream.ReadRawInt32();
            EntityId = stream.ReadRawVarint64();
            IsUsingGamepadInput = boolDecoder.ReadBool(stream);
            AvatarWorldInstanceId = stream.ReadRawVarint32();
            LocFlags = stream.ReadRawVarint32().ToBoolArray(LocFlagCount);
            Position = new(stream, 3);
            if (LocFlags[0])
                Orientation = new(stream, 6);
            else
                Orientation = new(stream.ReadRawZigZagFloat(6), 0f, 0f);
            LocomotionState = new(stream, LocFlags);
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
                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawInt32(AvatarIndex);
                cos.WriteRawVarint64(EntityId);
                boolEncoder.WriteBuffer(cos);   // IsUsingGamepadInput  
                cos.WriteRawVarint32(AvatarWorldInstanceId);
                cos.WriteRawVarint32(LocFlags.ToUInt32());
                Position.Encode(cos, 3);
                if (LocFlags[0])
                    Orientation.Encode(cos, 6);
                else
                    cos.WriteRawZigZagFloat(Orientation.X, 6);
                LocomotionState.Encode(cos, LocFlags);

                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"AvatarIndex: {AvatarIndex}");
            sb.AppendLine($"EntityId: {EntityId}");
            sb.AppendLine($"IsUsingGamepadInput: {IsUsingGamepadInput}");
            sb.AppendLine($"AvatarWorldInstanceId: {AvatarWorldInstanceId}");

            sb.Append("LocFlags: ");
            for (int i = 0; i < LocFlags.Length; i++) if (LocFlags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"Position: {Position}");
            sb.AppendLine($"Orientation: {Orientation}");
            sb.AppendLine($"LocomotionState: {LocomotionState}");
            return sb.ToString();
        }
    }
}
