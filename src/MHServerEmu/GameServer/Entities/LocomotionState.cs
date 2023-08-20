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

        public LocomotionState(CodedInputStream stream, bool[] flags)
        {
            if (flags[3]) LocomotionFlags = stream.ReadRawVarint64();
            if (flags[4]) Method = stream.ReadRawVarint32();
            if (flags[7]) MoveSpeed = stream.ReadRawFloat(0);
            if (flags[8]) Height = stream.ReadRawVarint32();
            if (flags[9]) FollowEntityId = stream.ReadRawVarint64();
            if (flags[10]) FollowEntityRange = new(stream.ReadRawFloat(0), stream.ReadRawFloat(0));

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
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                if (flags[3]) stream.WriteRawVarint64(LocomotionFlags);
                if (flags[4]) stream.WriteRawVarint32(Method);
                if (flags[7]) stream.WriteRawFloat(MoveSpeed, 0);
                if (flags[8]) stream.WriteRawVarint32(Height);
                if (flags[9]) stream.WriteRawVarint64(FollowEntityId);
                if (flags[10]) stream.WriteRawBytes(FollowEntityRange.Encode(0));

                if (flags[5])
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
