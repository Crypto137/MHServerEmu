using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Entities.Locomotion
{
    public class LocomotionPathNode
    {
        public Vector3 Vertex { get; set; }
        public int VertexSideRadius { get; set; }

        public LocomotionPathNode(CodedInputStream stream)
        {
            Vertex = new(stream, 3);
            VertexSideRadius = stream.ReadRawInt32();
        }

        public LocomotionPathNode(Vector3 vertex, int vertexSideRadius)
        {
            Vertex = vertex;
            VertexSideRadius = vertexSideRadius;
        }

        public void Encode(CodedOutputStream stream)
        {
            Vertex.Encode(stream, 3);
            stream.WriteRawInt32(VertexSideRadius);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Vertex: {Vertex}");
            sb.AppendLine($"VertexSideRadius: {VertexSideRadius}");
            return sb.ToString();
        }
    }
}
