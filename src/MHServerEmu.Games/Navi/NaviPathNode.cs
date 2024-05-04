using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public enum NaviSide
    {
        Left = 0,
        Right = 1,
        Point = 2
    }

    public class NaviPathNode
    {
        public Vector3 Vertex { get; set; }
        public NaviSide VertexSide { get; set; }
        public float Radius { get; set; }
        public bool HasInfluence { get; set; }
        // old
        public int VertexSideRadius { get; set; }

        public NaviPathNode(CodedInputStream stream)
        {
            Vertex = new(stream, 3);
            VertexSideRadius = stream.ReadRawInt32();
        }

        public NaviPathNode(Vector3 vertex, int vertexSideRadius)
        {
            Vertex = vertex;
            VertexSideRadius = vertexSideRadius;
        }

        public NaviPathNode(Vector3 vertex, NaviSide vertexSide, float radius, bool hasInfluence)
        {
            Vertex = vertex;
            VertexSide = vertexSide;
            Radius = radius;
            HasInfluence = hasInfluence;
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
