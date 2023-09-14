using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.Entities.Locomotion;

namespace MHServerEmu.GameServer.Entities.Avatars
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

        public UpdateAvatarStateArchive(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            ReplicationPolicy = stream.ReadRawVarint32();
            AvatarIndex = stream.ReadRawInt32();
            EntityId = stream.ReadRawVarint64();
            IsUsingGamepadInput = Convert.ToBoolean(stream.ReadRawVarint32());
            AvatarWorldInstanceId = stream.ReadRawVarint32();
            LocFlags = stream.ReadRawVarint32().ToBoolArray(LocFlagCount);
            Position = new(stream, 3);
            if (LocFlags[0])
                Orientation = new(stream, 6);
            else
                Orientation = new(stream.ReadRawFloat(6), 0f, 0f);
            LocomotionState = new(stream, LocFlags);
        }

        public UpdateAvatarStateArchive() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(ReplicationPolicy);
                cos.WriteRawInt32(AvatarIndex);
                cos.WriteRawVarint64(EntityId);
                cos.WriteRawVarint32(Convert.ToUInt32(IsUsingGamepadInput));
                cos.WriteRawVarint32(AvatarWorldInstanceId);
                cos.WriteRawVarint32(LocFlags.ToUInt32());
                cos.WriteRawBytes(Position.Encode());
                if (LocFlags[0])
                    cos.WriteRawBytes(Orientation.Encode(6));
                else
                    cos.WriteRawFloat(Orientation.X, 6);
                cos.WriteRawBytes(LocomotionState.Encode(LocFlags));

                cos.Flush();
                return ms.ToArray();
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
