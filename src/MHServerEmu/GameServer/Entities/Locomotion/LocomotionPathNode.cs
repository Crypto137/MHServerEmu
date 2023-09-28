using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Locomotion
{
    public class LocomotionPathNode
    {
        public Vector3 Vertex { get; set; }
        public int VertexSideRadius { get; set; }  // zigzag int

        public LocomotionPathNode(CodedInputStream stream)
        {
            Vertex = new(stream.ReadRawZigZagFloat(3), stream.ReadRawZigZagFloat(3), stream.ReadRawZigZagFloat(3));
            VertexSideRadius = stream.ReadRawInt32();
        }

        public LocomotionPathNode(Vector3 vertex, int vertexSideRadius)
        {
            Vertex = vertex;
            VertexSideRadius = vertexSideRadius;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawBytes(Vertex.Encode());
                cos.WriteRawInt32(VertexSideRadius);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Vertex: {Vertex}");
            sb.AppendLine($"VertexSideRadius: 0x{VertexSideRadius:X}");
            return sb.ToString();
        }
    }
}
