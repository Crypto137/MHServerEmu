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
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint32(ReplicationPolicy);
                stream.WriteRawInt32(AvatarIndex);
                stream.WriteRawVarint64(EntityId);
                stream.WriteRawVarint32(Convert.ToUInt32(IsUsingGamepadInput));
                stream.WriteRawVarint32(AvatarWorldInstanceId);
                stream.WriteRawVarint32(LocFlags.ToUInt32());
                stream.WriteRawBytes(Position.Encode());
                if (LocFlags[0])
                    stream.WriteRawBytes(Orientation.Encode(6));
                else
                    stream.WriteRawFloat(Orientation.X, 6);
                stream.WriteRawBytes(LocomotionState.Encode(LocFlags));

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"AvatarIndex: {AvatarIndex}");
            sb.AppendLine($"EntityId: 0x{EntityId:X}");
            sb.AppendLine($"IsUsingGamepadInput: {IsUsingGamepadInput}");
            sb.AppendLine($"AvatarWorldInstanceId: 0x{AvatarWorldInstanceId:X}");

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
