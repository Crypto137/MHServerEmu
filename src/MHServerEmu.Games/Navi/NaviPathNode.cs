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

    public class NaviPathNode   // TODO: Change to struct?
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
