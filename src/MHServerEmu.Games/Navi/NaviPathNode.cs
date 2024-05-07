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

    public class NaviPathNode   // TODO: Change to struct?
    {
        public Vector3 Vertex { get; set; }
        public NaviSide VertexSide { get; set; }
        public float Radius { get; set; }
        public bool HasInfluence { get; set; }

        public NaviPathNode() { }

        public void Decode(CodedInputStream stream, Vector3 previousVertex)
        {
            Vertex = new Vector3(stream, 3) + previousVertex;

            // Vertex side and radius are encoded together in the same value
            int vertexSideRadius = stream.ReadRawInt32();
            if (vertexSideRadius < 0)
            {
                VertexSide = NaviSide.Left;
                Radius = -vertexSideRadius;
            }
            else if (vertexSideRadius > 0)
            {
                VertexSide = NaviSide.Right;
                Radius = vertexSideRadius;
            }
            else /* if (vertexSideRadius == 0) */
            {
                VertexSide = NaviSide.Point;
                Radius = 0f;
            }
        }

        public NaviPathNode(Vector3 vertex, NaviSide vertexSide, float radius, bool hasInfluence)
        {
            Vertex = vertex;
            VertexSide = vertexSide;
            Radius = radius;
            HasInfluence = hasInfluence;
        }

        public void Encode(CodedOutputStream stream, Vector3 previousVertex)
        {
            Vector3 offset = Vertex - previousVertex;
            offset.Encode(stream, 3);

            // Combine vertex side with radius
            int vertexSideRadius = (int)MathF.Round(Radius);
            if (VertexSide == NaviSide.Left) vertexSideRadius = -vertexSideRadius;

            stream.WriteRawInt32(vertexSideRadius);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(Vertex)}: {Vertex}");
            sb.AppendLine($"{nameof(VertexSide)}: {VertexSide}");
            sb.AppendLine($"{nameof(Radius)}: {Radius}");
            return sb.ToString();
        }
    }
}
