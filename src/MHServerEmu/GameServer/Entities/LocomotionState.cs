using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities
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

        public LocomotionState(CodedInputStream stream, uint fieldFlags)
        {
            if ((fieldFlags & 0x8) > 0) LocomotionFlags = stream.ReadRawVarint64();
            if ((fieldFlags & 0x10) > 0) Method = stream.ReadRawVarint32();
            if ((fieldFlags & 0x80) > 0) MoveSpeed = stream.ReadRawFloat(0);
            if ((fieldFlags & 0x100) > 0) Height = stream.ReadRawVarint32();
            if ((fieldFlags & 0x200) > 0) FollowEntityId = stream.ReadRawVarint64();
            if ((fieldFlags & 0x400) > 0) FollowEntityRange = new(stream.ReadRawFloat(0), stream.ReadRawFloat(0));

            if ((fieldFlags & 0x20) > 0)
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

        public byte[] Encode(uint fieldFlags)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                if ((fieldFlags & 0x8) > 0) stream.WriteRawVarint64(LocomotionFlags);
                if ((fieldFlags & 0x10) > 0) stream.WriteRawVarint32(Method);
                if ((fieldFlags & 0x80) > 0) stream.WriteRawFloat(MoveSpeed, 0);
                if ((fieldFlags & 0x100) > 0) stream.WriteRawVarint32(Height);
                if ((fieldFlags & 0x200) > 0) stream.WriteRawVarint64(FollowEntityId);
                if ((fieldFlags & 0x400) > 0) stream.WriteRawBytes(FollowEntityRange.Encode(0));

                if ((fieldFlags & 0x20) > 0)
                {
                    stream.WriteRawVarint32(PathNodeUInt);
                    stream.WriteRawVarint64((ulong)LocomotionPathNodes.Length);
                    foreach (LocomotionPathNode naviVector in LocomotionPathNodes) stream.WriteRawBytes(naviVector.Encode());
                }

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"LocomotionFlags: 0x{LocomotionFlags.ToString("X")}");
                streamWriter.WriteLine($"Method: 0x{Method.ToString("X")}");
                streamWriter.WriteLine($"MoveSpeed: {MoveSpeed}");
                streamWriter.WriteLine($"Height: 0x{Height.ToString("X")}");
                streamWriter.WriteLine($"FollowEntityId: 0x{FollowEntityId.ToString("X")}");
                streamWriter.WriteLine($"FollowEntityRange: {FollowEntityRange}");
                streamWriter.WriteLine($"PathNodeUInt: 0x{PathNodeUInt.ToString("X")}");
                for (int i = 0; i < LocomotionPathNodes.Length; i++) streamWriter.WriteLine($"LocomotionPathNode{i}: {LocomotionPathNodes[i]}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
