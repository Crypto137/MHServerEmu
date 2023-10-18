using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Locomotion
{
    public class LocomotionState
    {
        public ulong LocomotionFlags { get; set; }
        public uint Method { get; set; }
        public float MoveSpeed { get; set; }
        public uint Height { get; set; }
        public ulong FollowEntityId { get; set; }
        public Vector2 FollowEntityRange { get; set; }
        public uint PathNodeUInt { get; set; }
        public LocomotionPathNode[] LocomotionPathNodes { get; set; } = Array.Empty<LocomotionPathNode>();

        public LocomotionState(CodedInputStream stream, bool[] flags)
        {
            if (flags[3]) LocomotionFlags = stream.ReadRawVarint64();
            if (flags[4]) Method = stream.ReadRawVarint32();
            if (flags[7]) MoveSpeed = stream.ReadRawZigZagFloat(0);
            if (flags[8]) Height = stream.ReadRawVarint32();
            if (flags[9]) FollowEntityId = stream.ReadRawVarint64();
            if (flags[10]) FollowEntityRange = new(stream.ReadRawZigZagFloat(0), stream.ReadRawZigZagFloat(0));

            if (flags[5])
            {
                PathNodeUInt = stream.ReadRawVarint32();
                LocomotionPathNodes = new LocomotionPathNode[stream.ReadRawVarint64()];
                for (int i = 0; i < LocomotionPathNodes.Length; i++)
                    LocomotionPathNodes[i] = new(stream);
            }
        }

        public LocomotionState(float moveSpeed)
        {
            MoveSpeed = moveSpeed;
        }

        public LocomotionState(ulong locomotionFlags, uint method, float moveSpeed, uint height,
            ulong followEntityId, Vector2 followEntityRange, uint pathNodeUInt, LocomotionPathNode[] locomotionPathNodes)
        {
            LocomotionFlags = locomotionFlags;
            Method = method;
            MoveSpeed = moveSpeed;
            Height = height;
            FollowEntityId = followEntityId;
            FollowEntityRange = followEntityRange;
            PathNodeUInt = pathNodeUInt;
            LocomotionPathNodes = locomotionPathNodes;
        }

        public byte[] Encode(bool[] flags)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                if (flags[3]) cos.WriteRawVarint64(LocomotionFlags);
                if (flags[4]) cos.WriteRawVarint32(Method);
                if (flags[7]) cos.WriteRawZigZagFloat(MoveSpeed, 0);
                if (flags[8]) cos.WriteRawVarint32(Height);
                if (flags[9]) cos.WriteRawVarint64(FollowEntityId);
                if (flags[10]) FollowEntityRange.Encode(cos, 0);

                if (flags[5])
                {
                    cos.WriteRawVarint32(PathNodeUInt);
                    cos.WriteRawVarint64((ulong)LocomotionPathNodes.Length);
                    foreach (LocomotionPathNode naviVector in LocomotionPathNodes) cos.WriteRawBytes(naviVector.Encode());
                }

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"LocomotionFlags: 0x{LocomotionFlags:X}");
            sb.AppendLine($"Method: 0x{Method:X}");
            sb.AppendLine($"MoveSpeed: {MoveSpeed}");
            sb.AppendLine($"Height: 0x{Height:X}");
            sb.AppendLine($"FollowEntityId: 0x{FollowEntityId:X}");
            sb.AppendLine($"FollowEntityRange: {FollowEntityRange}");
            sb.AppendLine($"PathNodeUInt: 0x{PathNodeUInt:X}");
            for (int i = 0; i < LocomotionPathNodes.Length; i++) sb.AppendLine($"LocomotionPathNode{i}: {LocomotionPathNodes[i]}");
            return sb.ToString();
        }
    }
}
