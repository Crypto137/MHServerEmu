using System.Text;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Navi
{
    public enum NaviSide
    {
        Left = 0,
        Right = 1,
        Point = 2
    }

    public class NaviPathNode   // We can't use struct here because it can be null --> we should just use nullable if that's the only issue
    {
        public Vector3 Vertex { get; set; }
        public NaviSide VertexSide { get; set; }
        public float Radius { get; set; }
        public bool HasInfluence { get; set; }

        public NaviPathNode() { }

        public NaviPathNode(Vector3 vertex, NaviSide vertexSide, float radius, bool hasInfluence)
        {
            Vertex = vertex;
            VertexSide = vertexSide;
            Radius = radius;
            HasInfluence = hasInfluence;
        }

        public NaviPathNode(NaviPathNode other)
        {
            // REMOVEME: Get rid of this when/if we turn it into a struct?
            Vertex = other.Vertex;
            VertexSide = other.VertexSide;
            Radius = other.Radius;
            HasInfluence = other.HasInfluence;
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
